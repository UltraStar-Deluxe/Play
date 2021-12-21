using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using UniInject;
using UnityEngine.EventSystems;
using System.Globalization;
using System.Threading;
using System.IO;
using System;
using TMPro;
using ProTrans;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneUiControl : MonoBehaviour, IOnHotSwapFinishedListener, INeedInjection, IBinder, ITranslator
{
    public static SongSelectSceneUiControl Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneUiControl>();
        }
    }

    [InjectedInInspector]
    public Sprite favoriteSprite;

    [InjectedInInspector]
    public Sprite noFavoriteSprite;

    [InjectedInInspector]
    public SongSelectSceneInputControl songSelectSceneInputControl;
    
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongRouletteControl songRouletteControl;

    [InjectedInInspector]
    public OrderSlider orderSlider;

    [InjectedInInspector]
    public CharacterQuickJumpBar characterQuickJumpBar;
    
    [InjectedInInspector]
    public SongSelectSceneControlNavigator songSelectSceneControlNavigator;

    [InjectedInInspector]
    public SongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public SongSelectPlayerListControl playerListControl;

    [InjectedInInspector]
    public SongSelectMicListControl micListControl;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.songIndexLabel)]
    private Label songIndexLabel;

    [Inject(UxmlName = R.UxmlNames.artistLabel)]
    private Label artistLabel;

    [Inject(UxmlName = R.UxmlNames.songTitle)]
    private Label songTitle;

    [Inject(UxmlName = R.UxmlNames.yearLabel)]
    private Label yearLabel;

    [Inject(UxmlName = R.UxmlNames.genreLabel)]
    private Label genreLabel;

    [Inject(UxmlName = R.UxmlNames.timesClearedLabel)]
    private Label timesClearedLabel;

    [Inject(UxmlName = R.UxmlNames.timesCanceledLabel)]
    private Label timesCanceledLabel;

    [Inject(UxmlName = R.UxmlNames.videoIcon)]
    private VisualElement videoIndicator;

    [Inject(UxmlName = R.UxmlNames.duetIcon)]
    private VisualElement duetIndicator;

    [Inject(UxmlName = R.UxmlNames.toggleFavoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.localHighScoreContainer)]
    private VisualElement localHighScoreContainer;

    [Inject(UxmlName = R.UxmlNames.onlineHighScoreContainer)]
    private VisualElement onlineHighScoreContainer;

    [Inject(UxmlName = R.UxmlNames.toggleFavoriteButton)]
    private Button toggleFavoriteButton;

    [Inject(UxmlName = R.UxmlNames.fuzzySearchTextLabel)]
    private Label fuzzySearchTextLabel;

    [Inject(UxmlName = R.UxmlNames.playerSelectOverlayContainer)]
    private VisualElement playerSelectOverlayContainer;

    [Inject(UxmlName = R.UxmlNames.closePlayerSelectOverlayButton)]
    private Button closePlayerSelectOverlayButton;

    [Inject(UxmlName = R.UxmlNames.leftLyricsOverlay)]
    private VisualElement leftLyricsOverlay;

    [Inject(UxmlName = R.UxmlNames.rightLyricsOverlay)]
    private VisualElement rightLyricsOverlay;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startButton;

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

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundLabel)]
    private VisualElement noSongsFoundLabel;

    private PlaylistChooserControl playlistChooserControl;

    public bool IsPlayerSelectOverlayVisible => playerSelectOverlayContainer.IsVisibleByDisplay();

    private SongMeta SelectedSong
    {
        get
        {
            return songRouletteControl.Selection.Value.SongMeta;
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

        playlistChooserControl = new PlaylistChooserControl();
        injector.Inject(playlistChooserControl);

        GetSongMetasFromManager();

        songRouletteControl.SelectionClickedEventStream
            .Subscribe(selection => CheckAudioAndStartSingScene());

        // Show a message when no songs have been found.
        noSongsFoundLabel.SetVisibleByDisplay(songMetas.IsNullOrEmpty());

        playlistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());
        // orderSlider.Selection.Subscribe(_ => UpdateFilteredSongs());
        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == playlistChooserControl.Selection.Value)
            {
                UpdateFilteredSongs();
            }

            UpdateFavoriteIcon();
        });

        toggleFavoriteButton.RegisterCallbackButtonTriggered(() => ToggleSelectedSongIsFavorite());

        closePlayerSelectOverlayButton.RegisterCallbackButtonTriggered(() => HidePlayerSelectOverlay());

        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.text = newValue);

        InitSongRoulette();

        HidePlayerSelectOverlay();

        startButton.RegisterCallbackButtonTriggered(() => CheckAudioAndStartSingScene());
    }

    private void UpdateFavoriteIcon()
    {
        Sprite sprite = IsFavorite(SelectedSong)
            ? favoriteSprite
            : noFavoriteSprite;
        favoriteIcon.style.backgroundImage = new StyleBackground(sprite);
    }

    public void GetSongMetasFromManager()
    {
        songMetas = new List<SongMeta>(SongMetaManager.Instance.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        noSongsFoundLabel.SetVisibleByDisplay(songMetas.IsNullOrEmpty());
    }

    void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (songMetas.Count != SongMetaManager.Instance.GetSongMetas().Count
            && !IsSearchEnabled()
            && lastSongMetasReloadFrame + 10 < Time.frameCount)
        {
            GetSongMetasFromManager();
            SongMeta selectedSong = songRouletteControl.Selection.Value.SongMeta;
            InitSongRoulette();
            songRouletteControl.SelectSong(selectedSong);
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
            songRouletteControl.SelectSong(sceneData.SongMeta);
        }

        songRouletteControl.Selection.Subscribe(newValue => OnNewSongSelection(newValue));
    }

    private void OnNewSongSelection(SongSelection selection)
    {
        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            return;
        }

        artistLabel.text = selectedSong.Artist;
        songTitle.text = selectedSong.Title;
        genreLabel.text = selectedSong.Genre;
        yearLabel.text = selectedSong.Year > 0
            ? selectedSong.Year.ToString()
            : "";
        songIndexLabel.text = (selection.SongIndex + 1) + "\nof " + selection.SongsCount;

        bool hasVideo = !string.IsNullOrEmpty(selectedSong.Video);
        videoIndicator.SetVisibleByVisibility(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIndicator.SetVisibleByVisibility(isDuet);

        UpdateFavoriteIcon();

        UpdateSongStatistics(selectedSong);
    }

    private void UpdateSongStatistics(SongMeta songMeta)
    {
        LocalStatistic localStatistic = statistics.GetLocalStats(songMeta);
        if (localStatistic != null)
        {
            timesClearedLabel.text = $"Cleared: {localStatistic.TimesFinished.ToString()}";
            timesCanceledLabel.text = $"Canceled: {localStatistic.TimesCanceled.ToString()}";

            List<SongStatistic> topScores = localStatistic.StatsEntries.GetTopScores(3);
            List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();
            UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);

            UpdateTopScoreLabels(new List<int>(), onlineHighScoreContainer);
        }
        else
        {
            timesClearedLabel.text = "";
            timesCanceledLabel.text = "";

            localHighScoreContainer.Q<Label>(R.UxmlNames.first).text = "";
            localHighScoreContainer.Q<Label>(R.UxmlNames.second).text = "";
            localHighScoreContainer.Q<Label>(R.UxmlNames.third).text = "";
        }
    }

    private void UpdateTopScoreLabels(List<int> topScores, VisualElement labelContainer)
    {
        string firstScore = topScores.Count >= 1
            ? topScores[0].ToString()
            : "-";
        string secondScore = topScores.Count >= 2
            ? topScores[1].ToString()
            : "-";
        string thirdScore = topScores.Count >= 3
            ? topScores[2].ToString()
            : "-";
        labelContainer.Q<Label>(R.UxmlNames.first).text = firstScore;
        labelContainer.Q<Label>(R.UxmlNames.second).text = secondScore;
        labelContainer.Q<Label>(R.UxmlNames.third).text = thirdScore;
    }

    public void DoFuzzySearch(string text)
    {
        string searchTextToLowerNoWhitespace = text.ToLowerInvariant().Replace(" ", "");
        if (searchTextToLowerNoWhitespace.IsNullOrEmpty())
        {
            return;
        }

        // Try to jump to song-index
        if (TryExecuteSpecialSearchSyntax(text))
        {
            return;
        }
        
        // Search title that starts with the text
        SongMeta titleStartsWithMatch = songRouletteControl.Find(it =>
        {
            string titleToLowerNoWhitespace = it.Title.ToLowerInvariant().Replace(" ", "");
            return titleToLowerNoWhitespace.StartsWith(searchTextToLowerNoWhitespace);
        });
        if (titleStartsWithMatch != null)
        {
            songRouletteControl.SelectSong(titleStartsWithMatch);
            return;
        }
        
        // Search artist that starts with the text
        SongMeta artistStartsWithMatch = songRouletteControl.Find(it =>
        {
            string artistToLowerNoWhitespace = it.Artist.ToLowerInvariant().Replace(" ", "");
            return artistToLowerNoWhitespace.StartsWith(searchTextToLowerNoWhitespace);
        });
        if (artistStartsWithMatch != null)
        {
            songRouletteControl.SelectSong(artistStartsWithMatch);
            return;
        }
        
        // Search title or artist contains the text
        SongMeta artistOrTitleContainsMatch = songRouletteControl.Find(it =>
        {
            string artistToLowerNoWhitespace = it.Artist.ToLowerInvariant().Replace(" ", "");
            string titleToLowerNoWhitespace = it.Title.ToLowerInvariant().Replace(" ", "");
            return artistToLowerNoWhitespace.Contains(searchTextToLowerNoWhitespace)
                || titleToLowerNoWhitespace.Contains(searchTextToLowerNoWhitespace);
        });
        if (artistOrTitleContainsMatch != null)
        {
            songRouletteControl.SelectSong(artistOrTitleContainsMatch);
        }
    }

    private SingSceneData CreateSingSceneData(SongMeta songMeta)
    {
        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = songMeta;

        List<PlayerProfile> selectedPlayerProfiles = playerListControl.GetSelectedPlayerProfiles();
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            uiManager.CreateNotificationVisualElement(TranslationManager.GetTranslation(R.Messages.songSelectScene_noPlayerSelected_title));
            return null;
        }
        singSceneData.SelectedPlayerProfiles = selectedPlayerProfiles;

        singSceneData.PlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();
        singSceneData.PlayerProfileToVoiceNameMap = playerListControl.GetSelectedPlayerProfileToVoiceNameMap();
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
        artistLabel.text = "";
        songTitle.text = "";
        genreLabel.text = "";
        yearLabel.text = "";
        timesClearedLabel.text = "";
        timesCanceledLabel.text = "";
        videoIndicator.SetVisibleByDisplay(false);
        duetIndicator.SetVisibleByDisplay(false);
        UpdateFavoriteIcon();
    }

    public void OnRandomSong()
    {
        songRouletteControl.SelectRandomSong();
    }

    public void CheckAudioAndStartSingScene()
    {
        if (playerSelectOverlayContainer.IsVisibleByDisplay())
        {
            StartSingScene(SelectedSong);
        }
        else if (SelectedSong.VoiceNames.Count <= 1
                 && playerListControl.PlayerEntryControlControls.Count == 1
                 && micListControl.MicEntryControls.Count == 1)
        {
            // There is one mic for only one player and only one voice to sing.
            // Thus, there is no choice to make and the song can be started immediately.
            playerListControl.PlayerEntryControlControls[0].MicProfile = micListControl.MicEntryControls[0].MicProfile;
            playerListControl.PlayerEntryControlControls[0].SetSelected(true);
            StartSingScene(SelectedSong);
        }
        else
        {
            if (SelectedSong == null)
            {
                return;
            }

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

            ShowPlayerSelectOverlay();
        }
    }

    private void ShowPlayerSelectOverlay()
    {
        playerSelectOverlayContainer.ShowByDisplay();

        // Show lyrics for duet song
        bool hasMultipleVoices = SelectedSong.VoiceNames.Count > 1;
        leftLyricsOverlay.SetVisibleByDisplay(hasMultipleVoices);
        rightLyricsOverlay.SetVisibleByDisplay(hasMultipleVoices);
        if (hasMultipleVoices)
        {
            List<string> voiceNames = SelectedSong.VoiceNames.Values.ToList();
            leftLyricsOverlay.Q<Label>(R.UxmlNames.voiceNameLabel).text = voiceNames[0];
            leftLyricsOverlay.Q<Label>(R.UxmlNames.lyricsLabel).text = SongMetaUtils.GetLyrics(SelectedSong, SelectedSong.VoiceNames[Voice.firstVoiceName]);

            rightLyricsOverlay.Q<Label>(R.UxmlNames.voiceNameLabel).text = voiceNames[1];
            rightLyricsOverlay.Q<Label>(R.UxmlNames.lyricsLabel).text = SongMetaUtils.GetLyrics(SelectedSong, SelectedSong.VoiceNames[Voice.secondVoiceName]);

            playerListControl.ShowVoiceSelection(SelectedSong);
        }
        else
        {
            playerListControl.HideVoiceSelection();
        }

        // Focus start button, such that it can be triggered by keyboard
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () =>
            playerSelectOverlayContainer.Q<Button>(R.UxmlNames.startButton).Focus()));
    }

    public void HidePlayerSelectOverlay()
    {
        playerSelectOverlayContainer.HideByDisplay();
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
                songRouletteControl.SelectSong(lastSelectedSong);
            }
            else if (selectedSongBeforeSearch != null)
            {
                songRouletteControl.SelectSong(selectedSongBeforeSearch);
            }
        }
    }

    public List<SongMeta> GetFilteredSongMetas()
    {
        string searchText = IsSearchEnabled() ? GetSearchText().TrimStart() : null;
        UltraStarPlaylist playlist = playlistChooserControl.Selection.Value;
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
        return songMeta.Artist;
        // switch (orderSlider.SelectedItem)
        // {
        //     case ESongOrder.Artist:
        //         return songMeta.Artist;
        //     case ESongOrder.Title:
        //         return songMeta.Title;
        //     case ESongOrder.Genre:
        //         return songMeta.Genre;
        //     case ESongOrder.Language:
        //         return songMeta.Language;
        //     case ESongOrder.Folder:
        //         return songMeta.Directory + "/" + songMeta.Filename;
        //     case ESongOrder.Year:
        //         return songMeta.Year;
        //     case ESongOrder.CountStarted:
        //         return statistics.GetLocalStats(songMeta)?.TimesStarted;
        //     case ESongOrder.CountFinished:
        //         return statistics.GetLocalStats(songMeta)?.TimesFinished;
        //     default:
        //         // See end of method
        //         break;
        // }
        // Debug.LogWarning("Unkown order for songs: " + orderSlider.SelectedItem);
        // return songMeta.Artist;
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
        // return eventSystem.currentSelectedGameObject == searchTextInputField.GetInputField().gameObject;
        return false;
    }

    public bool IsSearchTextInputHasFocus()
    {
        // return eventSystem.currentSelectedGameObject == searchTextInputField.GetInputField().gameObject;
        return false;
    }

    public void ToggleSelectedPlayers()
    {
        playerListControl.ToggleSelectedPlayers();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(songRouletteControl);
        bb.BindExistingInstance(songSelectSceneInputControl);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(orderSlider);
        bb.BindExistingInstance(characterQuickJumpBar);
        bb.BindExistingInstance(playerListControl);
        bb.BindExistingInstance(songSelectSceneControlNavigator);
        bb.BindExistingInstance(songPreviewControl);
        return bb.GetBindings();
    }

    public void ToggleFavoritePlaylist()
    {
        playlistChooserControl.ToggleFavoritePlaylist();
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
        songRouletteControl.SetSongs(GetFilteredSongMetas());

        // Indicate filtered playlist via font style of song count
        songIndexLabel.style.unityFontStyleAndWeight = IsPlaylistActive()
            ? new StyleEnum<FontStyle>(FontStyle.BoldAndItalic)
            : new StyleEnum<FontStyle>(FontStyle.Normal);
    }

    public bool IsPlaylistActive()
    {
        return playlistChooserControl.Selection.Value != null
               && !(playlistChooserControl.Selection.Value is UltraStarAllSongsPlaylist);
    }

    public void ResetPlaylistSelection()
    {
        playlistChooserControl.Reset();
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
                songRouletteControl.SelectSongByIndex(number - 1, false);
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

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && sceneTitle == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_title);
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
               && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title);
    }
}
