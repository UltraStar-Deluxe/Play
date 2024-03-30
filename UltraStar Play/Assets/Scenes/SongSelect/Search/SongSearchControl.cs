using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
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

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

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

    [Inject(UxmlName = R.UxmlNames.yearPropertyToggle)]
    private Toggle yearPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.editionPropertyToggle)]
    private Toggle editionPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.languagePropertyToggle)]
    private Toggle languagePropertyToggle;

    [Inject(UxmlName = R.UxmlNames.lyricsPropertyToggle)]
    private Toggle lyricsPropertyToggle;

    [Inject(UxmlName = R.UxmlNames.searchErrorIcon)]
    private VisualElement searchErrorIcon;

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

    private TooltipControl searchErrorIconTooltipControl;

    public bool IsSearchPropertyDropdownVisible => searchPropertyDropdownOverlay.IsVisibleByDisplay();

    private HashSet<ESearchProperty> searchProperties = new();

    private readonly Subject<SearchChangedEvent> searchChangedEventStream = new();
    public IObservable<SearchChangedEvent> SearchChangedEventStream => searchChangedEventStream;

    private readonly Subject<bool> submitEventStream = new();
    public IObservable<bool> SubmitEventStream => submitEventStream;

    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSearchControl.OnInjectionFinished");

        searchProperties = new HashSet<ESearchProperty>(settings.SearchProperties);
        searchTextField.RegisterValueChangedCallback(evt =>
        {
            searchChangedEventStream.OnNext(new SearchTextChangedEvent());
        });
        searchTextField.DisableParseEscapeSequences();
        searchTextField.RegisterCallback<NavigationSubmitEvent>(_ => submitEventStream.OnNext(true));
        new TextFieldHintControl(searchTextFieldHint);

        songSelectSceneInputControl.FuzzySearchText.Subscribe(newValue => searchTextFieldHint.SetVisibleByVisibility(newValue.IsNullOrEmpty()));

        searchErrorIcon.HideByDisplay();
        searchErrorIconTooltipControl = new(searchErrorIcon);

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
        RegisterToggleSearchPropertyCallback(genrePropertyToggle, ESearchProperty.Genre);
        RegisterToggleSearchPropertyCallback(yearPropertyToggle, ESearchProperty.Year);
        RegisterToggleSearchPropertyCallback(editionPropertyToggle, ESearchProperty.Edition);
        RegisterToggleSearchPropertyCallback(languagePropertyToggle, ESearchProperty.Language);
        RegisterToggleSearchPropertyCallback(lyricsPropertyToggle, ESearchProperty.Lyrics);

        new AnchoredPopupControl(searchPropertyDropdownContainer, searchPropertyButton, Corner2D.BottomRight);
        new UseAvailableScreenHeightControl(searchPropertyDropdownContainer);

        songRouletteControl.EntryListChangedEventStream
            .Subscribe(_ => UpdateSearchTextFieldStyle());
        songSelectSceneControl.IsSongRepositorySearchRunning
            .Subscribe(_ => UpdateSearchTextFieldStyle());

        TranslationManager.ApplyTranslations(visualElement);
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(visualElement);
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
        searchTextField.value = "";
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

    private string GetTranslation(ESearchProperty searchProperty)
    {
        switch (searchProperty)
        {
            case ESearchProperty.Artist:
                return Translation.Get(R.Messages.songProperty_artist);
            case ESearchProperty.Title:
                return Translation.Get(R.Messages.songProperty_title);
            case ESearchProperty.Year:
                return Translation.Get(R.Messages.songProperty_year);
            case ESearchProperty.Genre:
                return Translation.Get(R.Messages.songProperty_genre);
            case ESearchProperty.Language:
                return Translation.Get(R.Messages.songProperty_language);
            case ESearchProperty.Edition:
                return Translation.Get(R.Messages.songProperty_edition);
            case ESearchProperty.Lyrics:
                return Translation.Get(R.Messages.songProperty_lyrics);
            default:
                return searchProperty.ToString();
        }
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
        searchErrorIcon.HideByDisplay();
        if (IsSearchExpression(searchExp))
        {
            try
            {
                List<SongMeta> searchExpSongMetas = songMetas.AsQueryable()
                    .Where(searchExp)
                    .ToList();
                return searchExpSongMetas;
            }
            catch (Exception e)
            {
                Debug.Log($"Invalid search expression '{searchExp}': {e.Message}. Stack trace:\n{e.StackTrace}");
                searchErrorIcon.ShowByDisplay();
                searchErrorIconTooltipControl.TooltipText = Translation.Get(R.Messages.songSelectScene_searchExpressionError,
                    "errorDetails", e.Message);
                return new List<SongMeta>();
            }
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

    private bool IsSearchExpression(string searchExp)
    {
        bool IsSongPropertyRelation(ESongProperty songProperty)
        {
            List<string> relations = new() { "=", "!=", "<", ">", ">=", "<=" };
            List<string> methods = new() { ".Contains(", ".StartsWith(", ".EndsWith(",
                ".ToLower(", ".ToUpper(", ".ToLowerInvariant(", ".ToUpperInvariant(" };
            string searchExpNoWhitespace = searchExp.Replace(" ", "");
            return relations.AnyMatch(relation =>
                       searchExpNoWhitespace.StartsWith($"{songProperty}{relation}")
                       || searchExpNoWhitespace.Contains($"{relation}{songProperty}"))
                   || methods.AnyMatch(boolMethod =>
                       searchExpNoWhitespace.StartsWith($"{songProperty}{boolMethod}"));
        }

        return !searchExp.IsNullOrEmpty()
               && EnumUtils.GetValuesAsList<ESongProperty>().AnyMatch(songProperty => IsSongPropertyRelation(songProperty));
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

    public bool IsSearchTextFieldFocused()
    {
        return searchTextField.focusController.focusedElement == searchTextField
               || !searchTextField.value.IsNullOrEmpty();
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
        searchTextField.Blur();
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
