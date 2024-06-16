using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
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

    [Inject(UxmlName = R.UxmlNames.filtersAccordionItem)]
    private AccordionItem filtersAccordionItem;

    private bool isInitialized;

    private Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> ActiveFilters => nonPersistentSettings.ActiveSearchPropertyFilters;
    public bool IsAnyFilterActive => !nonPersistentSettings.ActiveSearchPropertyFilters.IsNullOrEmpty()
        || nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value;

    private readonly Subject<VoidEvent> filtersChangedEventStream = new();
    public IObservable<VoidEvent> FiltersChangedEventStream => filtersChangedEventStream;

    private List<Toggle> filterToggles = new();

    public void OnInjectionFinished()
    {
        FieldBindingUtils.Bind(gameObject, showOnlyDuetsToggle,
            () => nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value,
            newValue =>
            {
                nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value = newValue;
                filtersChangedEventStream.OnNext(VoidEvent.instance);
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

        if (songMetaManager.IsSongScanFinished)
        {
            UpdateFilterList();
        }
        else
        {
            filterListContainer.Clear();
            songMetaManager.SongScanFinishedEventStream
                .Subscribe(_ => UpdateFilterList());
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
        return StringUtils.ContainsIgnoreCaseAndDiacritics(songMetaValue, searchPropertyFilter.value);
    }

    private void UpdateFilterList()
    {
        filterListContainer.Clear();
        filterToggles.Clear();

        List<ESearchProperty> searchProperties = new()
        {
            ESearchProperty.Year,
            ESearchProperty.Language,
            ESearchProperty.Genre,
            ESearchProperty.Tags,
            ESearchProperty.Edition,
        };

        searchProperties.ForEach(searchProperty => FillFilterList(searchProperty));

        filtersAccordionItem.UpdateTargetHeight();

        filtersChangedEventStream.OnNext(VoidEvent.instance);
    }

    private void FillFilterList(ESearchProperty searchProperty)
    {
        List<string> values = songMetaManager.GetSongMetas()
            .Select(songMeta => GetSongMetaSearchProperty(songMeta, searchProperty).ToLowerInvariant())
            .SelectMany(value => IsCommaSeparatedSearchProperty(searchProperty) ? value.Split(",") : new []{ value })
            .Distinct(new StringEqualityComparerIgnoreCaseAndDiacritics())
            .Select(value => StringUtils.ToTitleCase(value.Trim()))
            .Where(value => !value.IsNullOrEmpty() && value != "Undefined" && value != "None" && value != "Unknown" && value != "0")
            .OrderBy(value => value)
            .ToList();

        if (values.IsNullOrEmpty())
        {
            // Cannot filter by this property
            return;
        }

        Label propertyLabel = new();
        propertyLabel.SetTranslatedText(Translation.Get(searchProperty));
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
    }

    private bool IsCommaSeparatedSearchProperty(ESearchProperty searchProperty)
    {
        return searchProperty
            is ESearchProperty.Language
            or ESearchProperty.Genre
            or ESearchProperty.Tags
            or ESearchProperty.Edition;
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
        filtersChangedEventStream.OnNext(VoidEvent.instance);
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
            case ESearchProperty.Tags:
                return songMeta.Tag;
            case ESearchProperty.Lyrics:
                return SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1, true);
            default:
                return null;
        }
    }

    public void Reset()
    {
        nonPersistentSettings.IsShowOnlyDuetsFilterActive.Value = false;
        ActiveFilters.ToList()
            .SelectMany(entry => entry.Value.ToList())
            .ForEach(activeFilter => DisableFilter(activeFilter));
        filtersChangedEventStream.OnNext(VoidEvent.instance);
    }
}
