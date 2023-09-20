using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectFilterControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject(UxmlName = R.UxmlNames.filterListContainer)]
    private VisualElement filterListContainer;

    [Inject(UxmlName = R.UxmlNames.showOnlyDuetsToggle)]
    private Toggle showOnlyDuetsToggle;

    [Inject(UxmlName = R.UxmlNames.showOnlyFilesWithoutSingAlongDataToggle)]
    private Toggle showOnlyFilesWithoutSingAlongDataToggle;

    [Inject(UxmlName = R.UxmlNames.filtersAccordionItem)]
    private AccordionItem filtersAccordionItem;

    private bool isInitialized;

    private Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> ActiveFilters => nonPersistentSettings.ActiveSearchPropertyFilters;
    public bool IsAnyFilterActive => !nonPersistentSettings.ActiveSearchPropertyFilters.IsNullOrEmpty()
        || nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value
        || nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value;

    private readonly Subject<bool> filtersChangedEventStream = new();
    public IObservable<bool> FiltersChangedEventStream => filtersChangedEventStream;

    private List<Toggle> filterToggles = new();

    public void OnInjectionFinished()
    {
        if (settings.SearchAudioFilesWithoutSongMeta)
        {
            showOnlyFilesWithoutSingAlongDataToggle.ShowByDisplay();
        }
        else
        {
            showOnlyFilesWithoutSingAlongDataToggle.HideByDisplay();
            nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value = false;
        }

        FieldBindingUtils.Bind(gameObject, showOnlyFilesWithoutSingAlongDataToggle,
            () => nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value,
            newValue =>
            {
                nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value = newValue;
                filtersChangedEventStream.OnNext(true);
            });

        FieldBindingUtils.Bind(gameObject, showOnlyDuetsToggle,
            () => nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value,
            newValue =>
            {
                nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value = newValue;
                filtersChangedEventStream.OnNext(true);
            });

        filtersAccordionItem.AfterContentVisibleChangedEventStream.Subscribe(_ => InitFilters());

        FiltersChangedEventStream.Subscribe(_ => OnFiltersChanged());
    }

    private void OnFiltersChanged()
    {
        HashSet<SearchPropertyFilter> activeFiltersHashSet = ActiveFilters
            .SelectMany(entry => entry.Value)
            .ToHashSet();
        foreach (Toggle filterToggle in filterToggles)
        {
            if (filterToggle.userData is SearchPropertyFilter searchPropertyFilterOfToggle
                && activeFiltersHashSet.Contains(searchPropertyFilterOfToggle))
            {
                // Should be checked
                if (!filterToggle.value)
                {
                    filterToggle.value = true;
                }
            }
            else
            {
                // Should not be checked
                if (filterToggle.value)
                {
                    filterToggle.value = false;
                }
            }
        }
    }

    public void InitFilters()
    {
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        if (SongMetaManager.IsSongScanFinished)
        {
            UpdateFilterList();
        }
        else
        {
            filterListContainer.Clear();
            songMetaManager.SongScanFinishedEventStream.Subscribe(_ => UpdateFilterList());
        }
    }

    public bool SongMetaPassesActiveFilters(SongMeta songMeta)
    {
        if (!IsAnyFilterActive)
        {
            return true;
        }

        if (nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value
            && songMeta.VoiceCount < 2)
        {
            return false;
        }

        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);
        if (nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value
            && !SongMetaUtils.HasNoSingAlongData(songMeta, generatedSongFolderAbsolutePath))
        {
            return false;
        }

        // EVERY category must match at least one value (i.e. return false if ANY does not match).
        foreach (ESearchProperty searchProperty in ActiveFilters.Keys)
        {
            if (!SongMetaPassesFilters(songMeta, searchProperty))
            {
                return false;
            }
        }

        return true;
    }

    private bool SongMetaPassesFilters(SongMeta songMeta, ESearchProperty searchProperty)
    {
        if (!ActiveFilters.ContainsKey(searchProperty))
        {
            return true;
        }

        // ANY value in the category must match (i.e. return true if ANY does match).
        HashSet<SearchPropertyFilter> searchPropertyFilters = ActiveFilters[searchProperty];
        foreach (SearchPropertyFilter searchPropertyFilter in searchPropertyFilters)
        {
            if (SongMetaPassesFilter(songMeta, searchPropertyFilter))
            {
                return true;
            }
        }

        return false;
    }

    private bool SongMetaPassesFilter(SongMeta songMeta, SearchPropertyFilter searchPropertyFilter)
    {
        string songMetaValue = GetSongMetaSearchProperty(songMeta, searchPropertyFilter.searchProperty);
        return songMetaValue.Equals(searchPropertyFilter.value, StringComparison.InvariantCultureIgnoreCase);
    }

    private void UpdateFilterList()
    {
        filterListContainer.Clear();
        filterToggles.Clear();

        List<ESearchProperty> searchProperties = new()
        {
            ESearchProperty.Language,
            ESearchProperty.Genre,
            ESearchProperty.Year,
            ESearchProperty.Edition,
        };

        searchProperties.ForEach(searchProperty => FillFilterList(searchProperty));

        filtersAccordionItem.UpdateTargetHeight();

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(filterListContainer);

        filtersChangedEventStream.OnNext(true);
    }

    private void FillFilterList(ESearchProperty searchProperty)
    {
        List<string> values = songMetaManager.GetSongMetas()
            .Select(songMeta => StringUtils.ToTitleCase(GetSongMetaSearchProperty(songMeta, searchProperty).ToLowerInvariant()))
            .Where(value => !value.IsNullOrEmpty() && value != "Undefined" && value != "None" && value != "Unknown" && value != "0")
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        Label propertyLabel = new(StringUtils.ToTitleCase(searchProperty.ToString()));
        propertyLabel.AddToClassList("searchFilterLabel");
        filterListContainer.Add(propertyLabel);
        foreach (string value in values)
        {
            Toggle filterToggle = new(value);
            filterToggle.AddToClassList("searchFilterToggle");
            filterListContainer.Add(filterToggle);

            SearchPropertyFilter searchPropertyFilter = new()
            {
                searchProperty = searchProperty,
                value = value,
            };

            if (nonPersistentSettings.ActiveSearchPropertyFilters.ContainsKey(searchPropertyFilter.searchProperty)
                && nonPersistentSettings.ActiveSearchPropertyFilters[searchPropertyFilter.searchProperty].Contains(searchPropertyFilter))
            {
                filterToggle.value = true;
                EnableFilter(searchPropertyFilter);
            }

            filterToggle.RegisterValueChangedCallback(evt => SetFilterActive(searchPropertyFilter, evt.newValue));
            filterToggle.userData = searchPropertyFilter;

            filterToggles.Add(filterToggle);
        }

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(filterListContainer);
    }

    private void DisableFilter(SearchPropertyFilter searchPropertyFilter)
    {
        if (ActiveFilters.ContainsKey(searchPropertyFilter.searchProperty))
        {
            // Remove from HashSet
            ActiveFilters[searchPropertyFilter.searchProperty].Remove(searchPropertyFilter);

            // Remove HashSet from Dictionary if empty
            if (ActiveFilters[searchPropertyFilter.searchProperty].IsNullOrEmpty())
            {
                ActiveFilters.Remove(searchPropertyFilter.searchProperty);
            }
        }
    }

    private void EnableFilter(SearchPropertyFilter searchPropertyFilter)
    {
        if (!ActiveFilters.ContainsKey(searchPropertyFilter.searchProperty))
        {
            // Create HashSet if none yet
            ActiveFilters.Add(searchPropertyFilter.searchProperty, new HashSet<SearchPropertyFilter>());
        }

        // Add to HashSet
        ActiveFilters[searchPropertyFilter.searchProperty].Add(searchPropertyFilter);
    }

    private void SetFilterActive(SearchPropertyFilter searchPropertyFilter, bool isActive)
    {
        if (isActive)
        {
            EnableFilter(searchPropertyFilter);
        }
        else
        {
            DisableFilter(searchPropertyFilter);
        }
        filtersChangedEventStream.OnNext(true);
    }

    private static string GetSongMetaSearchProperty(SongMeta songMeta, ESearchProperty searchProperty)
    {
        switch (searchProperty)
        {
            case ESearchProperty.Artist:
                return songMeta.Artist;
            case ESearchProperty.Title:
                return songMeta.Title;
            case ESearchProperty.Year:
                return songMeta.Year.ToString();
            case ESearchProperty.Genre:
                return songMeta.Genre;
            case ESearchProperty.Language:
                return songMeta.Language;
            case ESearchProperty.Edition:
                return songMeta.Edition;
            case ESearchProperty.Lyrics:
                return SongMetaUtils.GetLyrics(songMeta, Voice.firstVoiceId, true);
            default:
                return null;
        }
    }

    public void Reset()
    {
        nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value = false;
        nonPersistentSettings.IsShowOnlyFilesWithoutSingAlongDataFilterActive.Value = false;
        ActiveFilters.ToList()
            .SelectMany(entry => entry.Value.ToList())
            .ForEach(activeFilter => DisableFilter(activeFilter));
        filtersChangedEventStream.OnNext(true);
    }
}
