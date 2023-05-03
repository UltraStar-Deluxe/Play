using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongSelectFilterControl : INeedInjection, IInjectionFinishedListener
{
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

    private Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> ActiveFilters => nonPersistentSettings.activeSearchPropertyFilters;
    public bool IsAnyFilterActive => !nonPersistentSettings.activeSearchPropertyFilters.IsNullOrEmpty()
        || nonPersistentSettings.isShowOnlyDuetsFilterActive;
    
    private readonly Subject<bool> filtersChangedEventStream = new();
    public IObservable<bool> FiltersChangedEventStream => filtersChangedEventStream;
    
    public void OnInjectionFinished()
    {
        showOnlyDuetsToggle.value = nonPersistentSettings.isShowOnlyDuetsFilterActive;
        showOnlyDuetsToggle.RegisterValueChangedCallback(evt =>
        {
            nonPersistentSettings.isShowOnlyDuetsFilterActive = evt.newValue;
            filtersChangedEventStream.OnNext(true);
        });

        filtersAccordionItem.AfterContentVisibleChangedEventStream.Subscribe(_ => InitFilters());
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

        if (nonPersistentSettings.isShowOnlyDuetsFilterActive
            && songMeta.GetVoices().Count < 2)
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

        List<ESearchProperty> searchProperties = new List<ESearchProperty>()
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

            if (nonPersistentSettings.activeSearchPropertyFilters.ContainsKey(searchPropertyFilter.searchProperty)
                && nonPersistentSettings.activeSearchPropertyFilters[searchPropertyFilter.searchProperty].Contains(searchPropertyFilter))
            {
                filterToggle.value = true;
                EnableFilter(searchPropertyFilter);
            }
            
            filterToggle.RegisterValueChangedCallback(evt => SetFilterActive(searchPropertyFilter, evt.newValue));
        }
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
                return SongMetaUtils.GetLyrics(songMeta, Voice.firstVoiceName, true);
            default:
                return null;
        }
    }
}
