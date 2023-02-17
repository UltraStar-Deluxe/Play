using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongSelectFilterControl : INeedInjection
{
    [Inject]
    private SongMetaManager songMetaManager;
    
    [Inject(UxmlName = R.UxmlNames.filterListContainer)]
    private VisualElement filterListContainer;

    private bool isInitialized;

    private readonly Dictionary<ESearchProperty, HashSet<SearchPropertyFilter>> activeFilters = new();
    public bool IsAnyFilterActive => !activeFilters.IsNullOrEmpty();
    
    private readonly Subject<bool> filtersChangedEventStream = new();
    public IObservable<bool> FiltersChangedEventStream => filtersChangedEventStream;
    
    public void ShowFilters()
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

        // EVERY category must match at least one value (i.e. return false if ANY does not match).
        foreach (ESearchProperty searchProperty in activeFilters.Keys)
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
        if (!activeFilters.ContainsKey(searchProperty))
        {
            return true;
        }
        
        // ANY value in the category must match (i.e. return true if ANY does match).
        HashSet<SearchPropertyFilter> searchPropertyFilters = activeFilters[searchProperty];
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
        filterListContainer.Add(propertyLabel);
        foreach (string value in values)
        {
            Toggle filterToggle = new(value);
            filterListContainer.Add(filterToggle);
    
            SearchPropertyFilter searchPropertyFilter = new()
            {
                searchProperty = searchProperty,
                value = value,
            };
            filterToggle.RegisterValueChangedCallback(evt => ToggleFilter(searchPropertyFilter, evt.newValue));
        }
    }

    private void ToggleFilter(SearchPropertyFilter searchPropertyFilter, bool isActive)
    {
        if (isActive)
        {
            if (!activeFilters.ContainsKey(searchPropertyFilter.searchProperty))
            {
                activeFilters.Add(searchPropertyFilter.searchProperty, new HashSet<SearchPropertyFilter>());
            }

            activeFilters[searchPropertyFilter.searchProperty].Add(searchPropertyFilter);
        }
        else
        {
            if (activeFilters.ContainsKey(searchPropertyFilter.searchProperty))
            {
                activeFilters[searchPropertyFilter.searchProperty].Remove(searchPropertyFilter);
                if (activeFilters[searchPropertyFilter.searchProperty].IsNullOrEmpty())
                {
                    activeFilters.Remove(searchPropertyFilter.searchProperty);
                }
            }
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
    
    private struct SearchPropertyFilter
    {
        public ESearchProperty searchProperty;
        public string value;
    }
}
