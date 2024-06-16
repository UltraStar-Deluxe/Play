using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSearchControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private Settings settings;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject(UxmlName = R.UxmlNames.searchTextField)]
    private TextField searchTextField;

    [Inject(UxmlName = R.UxmlNames.searchTextFieldHint)]
    private Label searchTextFieldHint;

    [Inject(UxmlName = R.UxmlNames.searchPropertyButton)]
    private Button searchPropertyButton;

    [Inject(UxmlName = R.UxmlNames.filterActiveIcon)]
    private VisualElement filterActiveIcon;

    [Inject(UxmlName = R.UxmlNames.resetActiveFiltersButton)]
    private Button resetActiveFiltersButton;

    [Inject(UxmlName = R.UxmlNames.filterInactiveIcon)]
    private VisualElement filterInactiveIcon;

    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownOverlay)]
    private VisualElement searchPropertyDropdownOverlay;

    [Inject(UxmlName = R.UxmlNames.playlistDropdownField)]
    private DropdownField playlistDropdownField;

    [Inject(UxmlName = R.UxmlNames.artistPropertyToggle)]
    private Toggle artistPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.titlePropertyToggle)]
    private Toggle titlePropertyToggle;

    [Inject(UxmlName = R.UxmlNames.genrePropertyToggle)]
    private Toggle genrePropertyToggle;

    [Inject(UxmlName = R.UxmlNames.tagPropertyToggle)]
    private Toggle tagPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.yearPropertyToggle)]
    private Toggle yearPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.editionPropertyToggle)]
    private Toggle editionPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.languagePropertyToggle)]
    private Toggle languagePropertyToggle;

    [Inject(UxmlName = R.UxmlNames.lyricsPropertyToggle)]
    private Toggle lyricsPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.searchExpressionIcon)]
    private VisualElement searchExpressionIcon;

    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownContainer)]
    private VisualElement searchPropertyDropdownContainer;

    [Inject]
    private SongSelectionPlaylistChooserControl playlistChooserControl;

    [Inject]
    private Injector injector;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongSelectFilterControl songSelectFilterControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private SongSelectSceneInputControl songSelectSceneInputControl;

    private TooltipControl searchExpressionIconTooltipControl;

    public bool IsSearchPropertyDropdownVisible => searchPropertyDropdownOverlay.IsVisibleByDisplay();

    private HashSet<ESearchProperty> searchProperties = new();

    private readonly Subject<SearchChangedEvent> searchChangedEventStream = new();
    public IObservable<SearchChangedEvent> SearchChangedEventStream => searchChangedEventStream;

    private readonly Subject<VoidEvent> submitEventStream = new();
    public IObservable<VoidEvent> SubmitEventStream => submitEventStream;

    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSearchControl.OnInjectionFinished");

        searchProperties = new HashSet<ESearchProperty>(settings.SearchProperties);
        searchTextField.RegisterValueChangedCallback(evt =>
        {
            searchChangedEventStream.OnNext(new SearchTextChangedEvent());
        });
        searchTextField.DisableParseEscapeSequences();
        searchTextField.RegisterCallback<NavigationSubmitEvent>(_ => submitEventStream.OnNext(VoidEvent.instance));
        new TextFieldHintControl(searchTextFieldHint);

        songSelectSceneInputControl.FuzzySearchText.Subscribe(newValue => searchTextFieldHint.SetVisibleByVisibility(newValue.IsNullOrEmpty()));

        searchExpressionIcon.HideByDisplay();
        nonPersistentSettings.IsSearchExpressionsEnabled.Subscribe(newValue => searchExpressionIcon.SetVisibleByDisplay(newValue));
        searchExpressionIconTooltipControl = new(searchExpressionIcon);

        // Apply last search expression if any
        if (!nonPersistentSettings.LastValidSearchExpression.Value.IsNullOrEmpty())
        {
            searchTextField.value = nonPersistentSettings.LastValidSearchExpression.Value;
        }

        HideSearchPropertyDropdownOverlay();
        searchPropertyButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (IsSearchPropertyDropdownVisible)
            {
                HideSearchPropertyDropdownOverlay();
            }
            else
            {
                ShowSearchPropertyDropdownOverlay();
            }
        });
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(searchPropertyDropdownOverlay);

        filterActiveIcon.HideByDisplay();
        nonPersistentSettings.PlaylistName
            .Subscribe(_ => UpdateAnyFiltersActive());
        playlistManager.PlaylistChangeEventStream
            .Subscribe(_ => UpdateAnyFiltersActive());
        songSelectFilterControl.FiltersChangedEventStream
            .Subscribe(_ => UpdateAnyFiltersActive());
        resetActiveFiltersButton.RegisterCallbackButtonTriggered(_ => ResetActiveFilters());

        if (!nonPersistentSettings.ActiveSearchPropertyFilters.IsNullOrEmpty())
        {
            songSelectFilterControl.InitFilters();
        }

        RegisterToggleSearchPropertyCallback(artistPropertyToggle, ESearchProperty.Artist);
        RegisterToggleSearchPropertyCallback(titlePropertyToggle, ESearchProperty.Title);
        RegisterToggleSearchPropertyCallback(languagePropertyToggle, ESearchProperty.Language);
        RegisterToggleSearchPropertyCallback(genrePropertyToggle, ESearchProperty.Genre);
        RegisterToggleSearchPropertyCallback(tagPropertyToggle, ESearchProperty.Tags);
        RegisterToggleSearchPropertyCallback(editionPropertyToggle, ESearchProperty.Edition);
        RegisterToggleSearchPropertyCallback(yearPropertyToggle, ESearchProperty.Year);
        RegisterToggleSearchPropertyCallback(lyricsPropertyToggle, ESearchProperty.Lyrics);

        new AnchoredPopupControl(searchPropertyDropdownContainer, searchPropertyButton, Corner2D.BottomRight);
        new UseAvailableScreenHeightControl(searchPropertyDropdownContainer);

        songRouletteControl.EntryListChangedEventStream
            .Subscribe(_ => UpdateSearchTextFieldStyle());
        songSelectSceneControl.IsSongRepositorySearchRunning
            .Subscribe(_ => UpdateSearchTextFieldStyle());
    }

    private void UpdateSearchTextFieldStyle()
    {
        if (songRouletteControl.Entries.IsNullOrEmpty()
            && !GetRawSearchText().IsNullOrEmpty()
            && !songSelectSceneControl.IsSongRepositorySearchRunning.Value)
        {
            searchTextField.AddToClassList("noSearchResults");
        }
        else
        {
            searchTextField.RemoveFromClassList("noSearchResults");
        }
    }

    private void ResetActiveFilters()
    {
        playlistChooserControl.Reset();
        songSelectFilterControl.Reset();
        ResetSearchText();
    }

    private void UpdateAnyFiltersActive()
    {
        IPlaylist activePlaylist = playlistManager.GetPlaylistByName(nonPersistentSettings.PlaylistName.Value);
        bool isAnyFilterOrPlaylistActive = songSelectFilterControl.IsAnyFilterActive
                                           || (activePlaylist != null &&
                                               activePlaylist is not UltraStarAllSongsPlaylist);

        filterActiveIcon.SetVisibleByDisplay(isAnyFilterOrPlaylistActive);
        filterInactiveIcon.SetVisibleByDisplay(!isAnyFilterOrPlaylistActive);
    }

    public void ShowSearchPropertyDropdownOverlay()
    {
        searchPropertyDropdownOverlay.ShowByDisplay();
        playlistDropdownField.Focus();
    }

    public void HideSearchPropertyDropdownOverlay()
    {
        searchPropertyDropdownOverlay.HideByDisplay();
        searchPropertyButton.Focus();
    }

    public List<SongMeta> GetFilteredSongMetas(List<SongMeta> songMetas)
    {
        string searchExp = searchTextField.value;
        searchExpressionIcon.RemoveFromClassList("errorFontColor");
        searchExpressionIconTooltipControl.TooltipText = Translation.Get(R.Messages.songSelectScene_searchExpressionEnabled,
            "properties", GetAvailableSearchExpressionPropertiesCsv());
        if (nonPersistentSettings.IsSearchExpressionsEnabled.Value
            && !searchExp.IsNullOrEmpty())
        {
            try
            {
                List<SongMeta> searchExpSongMetas = songMetas.AsQueryable()
                    .Where(searchExp)
                    .ToList();
                nonPersistentSettings.LastValidSearchExpression.Value = searchExp;
                return searchExpSongMetas;
            }
            catch (Exception e)
            {
                Debug.Log($"Invalid search expression '{searchExp}': {e.Message}. Stack trace:\n{e.StackTrace}");
                searchExpressionIcon.AddToClassList("errorFontColor");
                searchExpressionIconTooltipControl.TooltipText = Translation.Get(R.Messages.songSelectScene_searchExpressionError,
                    "errorDetails", e.Message);
                return new List<SongMeta>();
            }
        }
        else
        {
            nonPersistentSettings.LastValidSearchExpression.Value = "";
        }

        // Ignore prefix for special search syntax
        string searchText = GetRawSearchText() != "#"
            ? GetSearchText()
            : "";
        List<SongMeta> filteredSongs = songMetas
            .Where(songMeta => searchText.IsNullOrEmpty()
                               || SongMetaMatchesSearchedProperties(songMeta, searchText))
            .ToList();
        return filteredSongs;
    }

    private string GetAvailableSearchExpressionPropertiesCsv()
    {
        return typeof(SongMeta).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(propertyName => propertyName)
            .JoinWith(", ");
    }

    private bool SongMetaMatchesSearchedProperties(SongMeta songMeta, string searchText)
    {
        if (songMeta == null)
        {
            return false;
        }

        if (searchText.IsNullOrEmpty())
        {
             return true;
        }

        if (searchProperties.Contains(ESearchProperty.Artist)
            && !songMeta.Artist.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Artist, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Title)
            && !songMeta.Title.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Title, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Genre)
            && !songMeta.Genre.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Genre, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Year)
            && songMeta.Year.ToString().ToLowerInvariant().Contains(searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Edition)
            && !songMeta.Edition.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Edition, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Tags)
            && !songMeta.Tag.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Tag, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Language)
            && !songMeta.Language.IsNullOrEmpty()
            && StringUtils.ContainsIgnoreCaseAndDiacritics(songMeta.Language, searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Lyrics)
            && SongMetaMatchesLyrics(songMeta, searchText))
        {
            return true;
        }
        return false;
    }

    private bool SongMetaMatchesLyrics(SongMeta songMeta, string searchText)
    {
        if (songMeta == null)
        {
            return false;
        }

        if (searchText.IsNullOrEmpty())
        {
            return true;
        }

        // TODO: Implement search on separate thread and concurrent update of search result.
        return songMeta.Voices
            .Select(voice => SongMetaUtils.GetLyrics(voice)
                // The character '~' is often used in UltraStar files to indicate a change of pitch during the same syllable.
                // Thus, it should be ignored when searching in lyrics.
                .Replace("~", ""))
            .Any(lyrics => StringUtils.ContainsIgnoreCaseAndDiacritics(lyrics, searchText));
    }

    public string GetRawSearchText()
    {
        return searchTextField.value;
    }

    public string GetSearchText()
    {
        return GetRawSearchText().TrimStart().ToLowerInvariant();
    }

    public void FocusSearchTextField()
    {
        searchTextField.Focus();
    }

    public void AddSearchProperty(ESearchProperty searchProperty)
    {
        searchProperties.Add(searchProperty);
        settings.SearchProperties = searchProperties.ToList();
        searchChangedEventStream.OnNext(new SearchPropertyChangedEvent());
    }

    public void RemoveSearchProperty(ESearchProperty searchProperty)
    {
        searchProperties.Remove(searchProperty);
        settings.SearchProperties = searchProperties.ToList();
        searchChangedEventStream.OnNext(new SearchPropertyChangedEvent());
    }

    public void SetSearchText(string newValue)
    {
        searchTextField.value = newValue;
    }

    public void ResetSearchText()
    {
        searchTextField.value = "";
    }

    private void RegisterToggleSearchPropertyCallback(Toggle toggle, ESearchProperty searchProperty)
    {
        toggle.value = settings.SearchProperties.Contains(searchProperty);
        toggle.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
            {
                AddSearchProperty(searchProperty);
            }
            else
            {
                RemoveSearchProperty(searchProperty);
            }
        });
    }

    ///////////////////////////////////////////////////////////
    public class SearchChangedEvent
    {

    }

    public class SearchPropertyChangedEvent : SearchChangedEvent
    {

    }

    public class SearchTextChangedEvent : SearchChangedEvent
    {

    }
}
