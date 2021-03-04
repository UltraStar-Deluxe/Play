using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UniRx;
using UniInject;
using UnityEngine.EventSystems;
using System.Globalization;
using System.Threading;
using System.IO;
using System;
using TMPro;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneController : MonoBehaviour, IOnHotSwapFinishedListener, INeedInjection, IBinder
{
    public static SongSelectSceneController Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneController>();
        }
    }

    [InjectedInInspector]
    public SongSelectSceneInputControl songSelectSceneInputControl;
    
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongRouletteController songRouletteController;

    [InjectedInInspector]
    public PlaylistSlider playlistSlider;

    [InjectedInInspector]
    public OrderSlider orderSlider;

    [InjectedInInspector]
    public CharacterQuickJumpBar characterQuickJumpBar;
    
    [InjectedInInspector]
    public SongSelectSceneControlNavigator songSelectSceneControlNavigator;

    [InjectedInInspector]
    public GraphicRaycaster graphicRaycaster;

    [InjectedInInspector]
    public SongPreviewController songPreviewController;
    
    public ArtistText artistText;
    public Text songTitleText;

    public TMP_Text songCountText;
    public GameObject videoIndicator;
    public GameObject duetIndicator;
    public FavoriteIndicator favoriteIndicator;

    public SongSelectPlayerProfileListController playerProfileListController;

    private SearchInputField searchTextInputField;

    private SongSelectSceneData sceneData;
    private List<SongMeta> songMetas;
    private int lastSongMetasReloadFrame = -1;
    private SongMeta selectedSongBeforeSearch;

    [Inject]
    private Statistics statistics;

    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private PlaylistManager playlistManager;

    public GameObject noSongsFoundMessage;
    
    private SongMeta SelectedSong
    {
        get
        {
            return songRouletteController.Selection.Value.SongMeta;
        }
    }

    void Start()
    {   
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!SongMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        sceneData = SceneNavigator.Instance.GetSceneData(CreateDefaultSceneData());

        searchTextInputField = GameObjectUtils.FindObjectOfType<SearchInputField>(true);

        GetSongMetasFromManager();

        songRouletteController.SelectionClickedEventStream
            .Subscribe(selection => CheckAudioAndStartSingScene());

        // Show a message when no songs have been found.
        noSongsFoundMessage.SetActive(songMetas.IsNullOrEmpty());

        playlistSlider.Selection.Subscribe(_ => UpdateFilteredSongs());
        orderSlider.Selection.Subscribe(_ => UpdateFilteredSongs());
        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == playlistSlider.SelectedItem)
            {
                UpdateFilteredSongs();
            }
        });

        InitSongRoulette();
    }

    public void GetSongMetasFromManager()
    {
        songMetas = new List<SongMeta>(SongMetaManager.Instance.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        noSongsFoundMessage.SetActive(songMetas.IsNullOrEmpty());
    }

    void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (songMetas.Count != SongMetaManager.Instance.GetSongMetas().Count
            && !IsSearchEnabled()
            && lastSongMetasReloadFrame + 10 < Time.frameCount)
        {
            GetSongMetasFromManager();
            SongMeta selectedSong = songRouletteController.Selection.Value.SongMeta;
            InitSongRoulette();
            songRouletteController.SelectSong(selectedSong);
        }
    }

    public void OnHotSwapFinished()
    {
        InitSongRoulette();
    }

    private void InitSongRoulette()
    {
        lastSongMetasReloadFrame = Time.frameCount;
        UpdateFilteredSongs();
        if (sceneData.SongMeta != null)
        {
            songRouletteController.SelectSong(sceneData.SongMeta);
        }

        songRouletteController.Selection.Subscribe(newValue => OnNewSongSelection(newValue));
    }

    private void OnNewSongSelection(SongSelection selection)
    {
        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            return;
        }

        artistText.SetText(selectedSong.Artist);
        songTitleText.text = selectedSong.Title;
        songCountText.text = (selection.SongIndex + 1) + "/" + selection.SongsCount;

        bool hasVideo = !string.IsNullOrEmpty(selectedSong.Video);
        videoIndicator.SetActive(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIndicator.SetActive(isDuet);
    }

    public void DoFuzzySearch(string text)
    {
        string searchTextToLowerNoWhitespace = text.ToLowerInvariant().Replace(" ", "");

        // Try to jump to song-index
        if (TryExecuteSpecialSearchSyntax(text))
        {
            return;
        }
        
        // Search title that starts with the text
        SongMeta titleStartsWithMatch = songRouletteController.Find(it =>
        {
            string titleToLowerNoWhitespace = it.Title.ToLowerInvariant().Replace(" ", "");
            return titleToLowerNoWhitespace.StartsWith(searchTextToLowerNoWhitespace);
        });
        if (titleStartsWithMatch != null)
        {
            songRouletteController.SelectSong(titleStartsWithMatch);
            return;
        }
        
        // Search artist that starts with the text
        SongMeta artistStartsWithMatch = songRouletteController.Find(it =>
        {
            string artistToLowerNoWhitespace = it.Artist.ToLowerInvariant().Replace(" ", "");
            return artistToLowerNoWhitespace.StartsWith(searchTextToLowerNoWhitespace);
        });
        if (artistStartsWithMatch != null)
        {
            songRouletteController.SelectSong(artistStartsWithMatch);
            return;
        }
        
        // Search title or artist contains the text
        SongMeta artistOrTitleContainsMatch = songRouletteController.Find(it =>
        {
            string artistToLowerNoWhitespace = it.Artist.ToLowerInvariant().Replace(" ", "");
            string titleToLowerNoWhitespace = it.Title.ToLowerInvariant().Replace(" ", "");
            return artistToLowerNoWhitespace.Contains(searchTextToLowerNoWhitespace)
                || titleToLowerNoWhitespace.Contains(searchTextToLowerNoWhitespace);
        });
        if (artistOrTitleContainsMatch != null)
        {
            songRouletteController.SelectSong(artistOrTitleContainsMatch);
        }
    }

    private SingSceneData CreateSingSceneData(SongMeta songMeta)
    {
        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = songMeta;

        List<PlayerProfile> selectedPlayerProfiles = playerProfileListController.GetSelectedPlayerProfiles();
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            UiManager.Instance.CreateWarningDialog(
                I18NManager.GetTranslation(R.String.songSelectScene_noPlayerSelected_title),
                I18NManager.GetTranslation(R.String.songSelectScene_noPlayerSelected_message));
            return null;
        }
        singSceneData.SelectedPlayerProfiles = selectedPlayerProfiles;

        PlayerProfileToMicProfileMap playerProfileToMicProfileMap = playerProfileListController.GetSelectedPlayerProfileToMicProfileMap();
        singSceneData.PlayerProfileToMicProfileMap = playerProfileToMicProfileMap;
        return singSceneData;
    }

    private void StartSingScene(SongMeta songMeta)
    {
        SingSceneData singSceneData = CreateSingSceneData(songMeta);
        if (singSceneData != null)
        {
            SceneNavigator.Instance.LoadScene(EScene.SingScene, singSceneData);
        }
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        SongEditorSceneData editorSceneData = new SongEditorSceneData();
        editorSceneData.SelectedSongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneData(songMeta);
        if (singSceneData != null)
        {
            editorSceneData.PlayerProfileToMicProfileMap = singSceneData.PlayerProfileToMicProfileMap;
            editorSceneData.SelectedPlayerProfiles = singSceneData.SelectedPlayerProfiles;
        }
        editorSceneData.PreviousSceneData = sceneData;
        editorSceneData.PreviousScene = EScene.SongSelectScene;

        SceneNavigator.Instance.LoadScene(EScene.SongEditorScene, editorSceneData);
    }

    private SongSelectSceneData CreateDefaultSceneData()
    {
        SongSelectSceneData sceneData = new SongSelectSceneData();
        return sceneData;
    }

    private void SetEmptySongDetails()
    {
        artistText.SetText("");
        songTitleText.text = "";
        songCountText.text = "0/0";
        videoIndicator.SetActive(false);
        duetIndicator.SetActive(false);
    }

    public void OnRandomSong()
    {
        songRouletteController.SelectRandomSong();
    }

    public void OnNextSong()
    {
        songRouletteController.SelectNextSong();
    }

    public void OnPreviousSong()
    {
        songRouletteController.SelectPreviousSong();
    }

    public void CheckAudioAndStartSingScene()
    {
        if (SelectedSong != null)
        {
            // Check that the audio file exists
            string audioPath = SongMetaUtils.GetAbsoluteSongAudioPath(SelectedSong);
            if (!File.Exists(audioPath))
            {
                UiManager.Instance.CreateWarningDialog("Audio Error", "Audio file does not exist: " + audioPath);
                return;
            }

            // Check that the used audio format can be loaded.
            songAudioPlayer.Init(SelectedSong);
            if (!songAudioPlayer.HasAudioClip)
            {
                UiManager.Instance.CreateWarningDialog("Audio Error", "Audio file could not be loaded.\nPlease use a supported format.");
                return;
            }

            StartSingScene(SelectedSong);
        }
    }

    public void StartSongEditorScene()
    {
        if (SelectedSong != null)
        {
            StartSongEditorScene(SelectedSong);
        }
    }

    public void OnSearchTextChanged()
    {
        SongMeta lastSelectedSong = SelectedSong;
        string rawSearchText = GetRawSearchText();
        if (TryExecuteSpecialSearchSyntax(rawSearchText))
        {
            // Special search syntax used. Do not perform normal filtering.
            return;
        }

        UpdateFilteredSongs();
        if (string.IsNullOrEmpty(GetSearchText()))
        {
            if (lastSelectedSong != null)
            {
                songRouletteController.SelectSong(lastSelectedSong);
            }
            else if (selectedSongBeforeSearch != null)
            {
                songRouletteController.SelectSong(selectedSongBeforeSearch);
            }
        }
    }

    public List<SongMeta> GetFilteredSongMetas()
    {
        string searchText = IsSearchEnabled() ? GetSearchText().TrimStart() : null;
        UltraStarPlaylist playlist = playlistSlider.SelectedItem;
        List<SongMeta> filteredSongs = songMetas
            .Where(songMeta => searchText.IsNullOrEmpty()
                               || songMeta.Title.ToLowerInvariant().Contains(searchText)
                               || songMeta.Artist.ToLowerInvariant().Contains(searchText))
            .Where(songMeta => playlist == null
                            || playlist.HasSongEntry(songMeta.Artist, songMeta.Title))
            .OrderBy(songMeta => GetSongMetaOrderByProperty(songMeta))
            .ToList();
        return filteredSongs;
    }

    private object GetSongMetaOrderByProperty(SongMeta songMeta)
    {
        switch (orderSlider.SelectedItem)
        {
            case ESongOrder.Artist:
                return songMeta.Artist;
            case ESongOrder.Title:
                return songMeta.Title;
            case ESongOrder.Genre:
                return songMeta.Genre;
            case ESongOrder.Language:
                return songMeta.Language;
            case ESongOrder.Folder:
                return songMeta.Directory + "/" + songMeta.Filename;
            case ESongOrder.Year:
                return songMeta.Year;
            case ESongOrder.CountStarted:
                return statistics.GetLocalStats(songMeta)?.TimesStarted;
            case ESongOrder.CountFinished:
                return statistics.GetLocalStats(songMeta)?.TimesFinished;
            default:
                // See end of method
                break;
        }
        Debug.LogWarning("Unkown order for songs: " + orderSlider.SelectedItem);
        return songMeta.Artist;
    }

    public void EnableSearch(SearchInputField.ESearchMode searchMode)
    {
        selectedSongBeforeSearch = SelectedSong;

        searchTextInputField.Show();
        searchTextInputField.RequestFocus();
        searchTextInputField.SearchMode = searchMode;
        searchTextInputField.Text = "";
    }

    public void DisableSearch()
    {
        searchTextInputField.Text = "";
        searchTextInputField.Hide();
        // Remove the focus from the search input text field
        EventSystem.current.SetSelectedGameObject(null);
    }

    public string GetRawSearchText()
    {
        return searchTextInputField.Text;
    }

    public string GetSearchText()
    {
        return GetRawSearchText().TrimStart().ToLowerInvariant();
    }

    public bool IsSearchEnabled()
    {
        return eventSystem.currentSelectedGameObject == searchTextInputField.GetInputField().gameObject;
    }

    public bool IsSearchTextInputHasFocus()
    {
        return eventSystem.currentSelectedGameObject == searchTextInputField.GetInputField().gameObject;
    }

    public void ToggleSelectedPlayers()
    {
        playerProfileListController.ToggleSelectedPlayers();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(songRouletteController);
        bb.BindExistingInstance(songSelectSceneInputControl);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(playlistSlider);
        bb.BindExistingInstance(orderSlider);
        bb.BindExistingInstance(characterQuickJumpBar);
        bb.BindExistingInstance(playerProfileListController);
        bb.BindExistingInstance(songSelectSceneControlNavigator);
        bb.BindExistingInstance(graphicRaycaster);
        bb.BindExistingInstance(songPreviewController);
        return bb.GetBindings();
    }

    public void ToggleFavoritePlaylist()
    {
        if (playlistSlider.SelectedItemIndex == 0)
        {
            playlistSlider.Selection.Value = playlistManager.FavoritesPlaylist;
        }
        else
        {
            playlistSlider.Selection.Value = playlistSlider.Items[0];
        }
    }

    public void ToggleSelectedSongIsFavorite()
    {
        if (SelectedSong == null)
        {
            return;
        }

        if (playlistManager.FavoritesPlaylist.HasSongEntry(SelectedSong.Artist, SelectedSong.Title))
        {
            playlistManager.RemoveSongFromPlaylist(playlistManager.FavoritesPlaylist, SelectedSong);
        }
        else
        {
            playlistManager.AddSongToPlaylist(playlistManager.FavoritesPlaylist, SelectedSong);
        }
    }

    public void UpdateFilteredSongs()
    {
        songRouletteController.SetSongs(GetFilteredSongMetas());

        // Indicate filtered playlist via font style of song count
        songCountText.fontStyle = playlistSlider.SelectedItem == null || playlistSlider.SelectedItem is UltraStarAllSongsPlaylist
            ? FontStyles.Normal
            : FontStyles.Underline;
    }

    private bool TryExecuteSpecialSearchSyntax(string searchText)
    {
        if (searchText != null && searchText.StartsWith("#"))
        {
            // #<number> jumps to song at index <number>.
            // The check for the special syntax has already been made, so we know the searchText starts with #.
            string numberString = searchText.Substring(1);
            if (int.TryParse(numberString, out int number))
            {
                songRouletteController.SelectSongByIndex(number - 1, false);
            }
            return true;
        }
        return false;
    }

    public SongMeta GetCharacterQuickJumpSongMeta(char character)
    {
        Predicate<char> matchPredicate;
        if (char.IsLetterOrDigit(character))
        {
            // Jump to song starts with character
            matchPredicate = (songCharacter) => songCharacter == character;
        }
        else if (character == '#')
        {
            // Jump to song starts with number
            matchPredicate = (songCharacter) => char.IsDigit(songCharacter);
        }
        else
        {
            // Jump to song starts with non-alphanumeric character
            matchPredicate = (songCharacter) => !char.IsLetterOrDigit(songCharacter);
        }

        SongMeta match = GetFilteredSongMetas()
            .Where(songMeta =>
            {
                string relevantString;
                if (orderSlider.SelectedItem == ESongOrder.Title)
                {
                    relevantString = songMeta.Title;
                }
                else if (orderSlider.SelectedItem == ESongOrder.Genre)
                {
                    relevantString = songMeta.Genre;
                }
                else if (orderSlider.SelectedItem == ESongOrder.Language)
                {
                    relevantString = songMeta.Language;
                }
                else if (orderSlider.SelectedItem == ESongOrder.Folder)
                {
                    relevantString = songMeta.Directory + "/" + songMeta.Filename;
                }
                else
                {
                    relevantString = songMeta.Artist;
                }
                return !relevantString.IsNullOrEmpty()
                    && matchPredicate.Invoke(relevantString.ToLowerInvariant()[0]);
            })
            .FirstOrDefault();
        return match;
    }
}
