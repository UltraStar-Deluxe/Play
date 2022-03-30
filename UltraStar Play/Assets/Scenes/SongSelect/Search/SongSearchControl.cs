using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSearchControl : INeedInjection, IInjectionFinishedListener, ITranslator
{
    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.searchTextField)]
    private TextField searchTextField;

    [Inject(UxmlName = R.UxmlNames.searchTextFieldHint)]
    private Label searchTextFieldHint;

    [Inject(UxmlName = R.UxmlNames.searchPropertyButton)]
    private Button searchPropertyButton;

    [Inject(UxmlName = R.UxmlNames.closeSearchPropertyDropdownButton)]
    private Button closeSearchPropertyDropdownButton;

    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownTitle)]
    private Label searchPropertyDropdownTitle;

    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownOverlay)]
    private VisualElement searchPropertyDropdownOverlay;

    [Inject(UxmlName = R.UxmlNames.artistPropertyContainer)]
    private VisualElement artistPropertyContainer;

    [Inject(UxmlName = R.UxmlNames.titlePropertyContainer)]
    private VisualElement titlePropertyContainer;

    [Inject(UxmlName = R.UxmlNames.genrePropertyContainer)]
    private VisualElement genrePropertyContainer;

    [Inject(UxmlName = R.UxmlNames.yearPropertyContainer)]
    private VisualElement yearPropertyContainer;

    [Inject(UxmlName = R.UxmlNames.editionPropertyContainer)]
    private VisualElement editionPropertyContainer;

    [Inject(UxmlName = R.UxmlNames.languagePropertyContainer)]
    private VisualElement languagePropertyContainer;

    [Inject(UxmlName = R.UxmlNames.lyricsPropertyContainer)]
    private VisualElement lyricsPropertyContainer;

    public bool IsSearchPropertyDropdownVisible => searchPropertyDropdownOverlay.IsVisibleByDisplay();

    private HashSet<ESearchProperty> searchProperties = new();

    private readonly Subject<SearchChangedEvent> searchChangedEventStream = new();
    public IObservable<SearchChangedEvent> SearchChangedEventStream => searchChangedEventStream;

    public void OnInjectionFinished()
    {
        searchProperties = new HashSet<ESearchProperty>(settings.SongSelectSettings.searchProperties);
        searchTextField.RegisterValueChangedCallback(evt =>
        {
            searchTextFieldHint.SetVisibleByDisplay(GetRawSearchText().IsNullOrEmpty());
            searchChangedEventStream.OnNext(new SearchTextChangedEvent());
        });

        HideSearchPropertyDropdownOverlay();
        searchPropertyButton.RegisterCallbackButtonTriggered(() =>
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
        closeSearchPropertyDropdownButton.RegisterCallbackButtonTriggered(() => searchPropertyDropdownOverlay.HideByDisplay());

        RegisterToggleSearchPropertyCallback(artistPropertyContainer.Q<Toggle>(), ESearchProperty.Artist);
        RegisterToggleSearchPropertyCallback(titlePropertyContainer.Q<Toggle>(), ESearchProperty.Title);
        RegisterToggleSearchPropertyCallback(genrePropertyContainer.Q<Toggle>(), ESearchProperty.Genre);
        RegisterToggleSearchPropertyCallback(yearPropertyContainer.Q<Toggle>(), ESearchProperty.Year);
        RegisterToggleSearchPropertyCallback(editionPropertyContainer.Q<Toggle>(), ESearchProperty.Edition);
        RegisterToggleSearchPropertyCallback(languagePropertyContainer.Q<Toggle>(), ESearchProperty.Language);
        RegisterToggleSearchPropertyCallback(lyricsPropertyContainer.Q<Toggle>(), ESearchProperty.Lyrics);

        SearchChangedEventStream
            .Where(evt => evt is SearchPropertyChangedEvent)
            .Subscribe(evt => UpdateTextFieldHint());

        UpdateTextFieldHint();
    }

    private void UpdateTextFieldHint()
    {
        List<string> searchPropertyStrings = searchProperties
            .Select(it => GetTranslation(it))
            .ToList();
        searchPropertyStrings.Sort();
        string hint = TranslationManager.GetTranslation(R.Messages.songSelectScene_searchTextFieldHint,
            "properties", string.Join(", ", searchPropertyStrings));
        searchTextFieldHint.text = hint;
    }

    private string GetTranslation(ESearchProperty searchProperty)
    {
        switch (searchProperty)
        {
            case ESearchProperty.Artist:
                return TranslationManager.GetTranslation(R.Messages.songProperty_artist);
            case ESearchProperty.Title:
                return TranslationManager.GetTranslation(R.Messages.songProperty_title);
            case ESearchProperty.Year:
                return TranslationManager.GetTranslation(R.Messages.songProperty_year);
            case ESearchProperty.Genre:
                return TranslationManager.GetTranslation(R.Messages.songProperty_genre);
            case ESearchProperty.Language:
                return TranslationManager.GetTranslation(R.Messages.songProperty_language);
            case ESearchProperty.Edition:
                return TranslationManager.GetTranslation(R.Messages.songProperty_edition);
            case ESearchProperty.Lyrics:
                return TranslationManager.GetTranslation(R.Messages.songProperty_lyrics);
            default:
                return searchProperty.ToString();
        }
    }

    public void ShowSearchPropertyDropdownOverlay()
    {
        searchPropertyDropdownOverlay.ShowByDisplay();
        artistPropertyContainer.Q<Toggle>().Focus();
    }

    public void HideSearchPropertyDropdownOverlay()
    {
        searchPropertyDropdownOverlay.HideByDisplay();
    }

    public List<SongMeta> GetFilteredSongMetas(List<SongMeta> songMetas)
    {
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

    private bool SongMetaMatchesSearchedProperties(SongMeta songMeta, string searchText)
    {
        if (searchProperties.Contains(ESearchProperty.Artist)
            && !songMeta.Artist.IsNullOrEmpty()
            && songMeta.Artist.ToLowerInvariant().Contains(searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Title)
            && !songMeta.Title.IsNullOrEmpty()
            && songMeta.Title.ToLowerInvariant().Contains(searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Genre)
            && !songMeta.Genre.IsNullOrEmpty()
            && songMeta.Genre.ToLowerInvariant().Contains(searchText))
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
            && songMeta.Edition.ToLowerInvariant().Contains(searchText))
        {
            return true;
        }
        if (searchProperties.Contains(ESearchProperty.Language)
            && !songMeta.Language.IsNullOrEmpty()
            && songMeta.Language.ToLowerInvariant().Contains(searchText))
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
        // TODO: Implement search on separate thread and concurrent update of search result.
        string searchTextLower = searchText.ToLowerInvariant();
        return songMeta.GetVoices()
            .Select(voice => SongMetaUtils.GetLyrics(songMeta, voice)
                // The character '~' is often used in UltraStar files to indicate a change of pitch during the same syllable.
                // Thus, it should be ignored when searching in lyrics.
                .Replace("~", ""))
            .Any(lyrics => lyrics.ToLowerInvariant().Contains(searchTextLower));
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
        settings.SongSelectSettings.searchProperties = searchProperties.ToList();
        searchChangedEventStream.OnNext(new SearchPropertyChangedEvent());
    }

    public void RemoveSearchProperty(ESearchProperty searchProperty)
    {
        searchProperties.Remove(searchProperty);
        settings.SongSelectSettings.searchProperties = searchProperties.ToList();
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
        toggle.value = settings.SongSelectSettings.searchProperties.Contains(searchProperty);
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

    public void UpdateTranslation()
    {
        searchPropertyDropdownTitle.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_searchPropertyDropdownTitle);
        artistPropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_artist);
        titlePropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_title);
        editionPropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_edition);
        genrePropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_genre);
        languagePropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_language);
        lyricsPropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_lyrics);
        yearPropertyContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.songProperty_year);
        UpdateTextFieldHint();
    }
}
