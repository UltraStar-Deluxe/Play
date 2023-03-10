using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
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

    [Inject(UxmlName = R.UxmlNames.searchErrorIcon)]
    private VisualElement searchErrorIcon;
    
    [Inject(UxmlName = R.UxmlNames.searchPropertyDropdownContainer)]
    private VisualElement searchPropertyDropdownContainer;

    [Inject]
    private Injector injector;

    private TooltipControl searchErrorIconTooltipControl;

    public bool IsSearchPropertyDropdownVisible => searchPropertyDropdownOverlay.IsVisibleByDisplay();

    private HashSet<ESearchProperty> searchProperties = new();

    private readonly Subject<SearchChangedEvent> searchChangedEventStream = new();
    public IObservable<SearchChangedEvent> SearchChangedEventStream => searchChangedEventStream;

    public void OnInjectionFinished()
    {
        searchProperties = new HashSet<ESearchProperty>(settings.SongSelectSettings.searchProperties);
        searchTextField.RegisterValueChangedCallback(evt =>
        {
            UpdateSearchTextFieldHint();
            searchChangedEventStream.OnNext(new SearchTextChangedEvent());
        });
        searchTextField.RegisterCallback<FocusEvent>(evt => UpdateSearchTextFieldHint());
        searchTextField.RegisterCallback<BlurEvent>(evt => UpdateSearchTextFieldHint());
        UpdateSearchTextFieldHint();

        searchErrorIcon.HideByDisplay();
        searchErrorIconTooltipControl = new(searchErrorIcon);

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
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(searchPropertyDropdownOverlay);

        RegisterToggleSearchPropertyCallback(artistPropertyContainer.Q<Toggle>(), ESearchProperty.Artist);
        RegisterToggleSearchPropertyCallback(titlePropertyContainer.Q<Toggle>(), ESearchProperty.Title);
        RegisterToggleSearchPropertyCallback(genrePropertyContainer.Q<Toggle>(), ESearchProperty.Genre);
        RegisterToggleSearchPropertyCallback(yearPropertyContainer.Q<Toggle>(), ESearchProperty.Year);
        RegisterToggleSearchPropertyCallback(editionPropertyContainer.Q<Toggle>(), ESearchProperty.Edition);
        RegisterToggleSearchPropertyCallback(languagePropertyContainer.Q<Toggle>(), ESearchProperty.Language);
        RegisterToggleSearchPropertyCallback(lyricsPropertyContainer.Q<Toggle>(), ESearchProperty.Lyrics);

        new AnchoredPopupControl(searchPropertyDropdownContainer, searchPropertyButton, Corner2D.BottomRight);
    }

    private void UpdateSearchTextFieldHint()
    {
        searchTextFieldHint.SetVisibleByDisplay(
            GetRawSearchText().IsNullOrEmpty()
            && searchTextField.focusController.focusedElement != searchTextField);
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
                searchErrorIconTooltipControl.TooltipText = TranslationManager.GetTranslation(R.Messages.songSelectScene_searchExpressionError,
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
        searchTextFieldHint.text = "What do you want to sing today?";
    }
}
