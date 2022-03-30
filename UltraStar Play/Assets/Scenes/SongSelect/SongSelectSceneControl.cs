﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneControl : MonoBehaviour, INeedInjection, IBinder, ITranslator, IInjectionFinishedListener
{
    public static SongSelectSceneControl Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneControl>();
        }
    }

    [InjectedInInspector]
    public VectorImage favoriteImageAsset;

    [InjectedInInspector]
    public VectorImage noFavoriteImageAsset;

    [InjectedInInspector]
    public SongSelectSceneInputControl songSelectSceneInputControl;
    
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongRouletteControl songRouletteControl;

    [InjectedInInspector]
    public CharacterQuickJumpListControl characterQuickJumpListControl;
    
    [InjectedInInspector]
    public SongSelectFocusableNavigator focusableNavigator;

    [InjectedInInspector]
    public SongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public SongSelectPlayerListControl playerListControl;

    [InjectedInInspector]
    public SongSelectMicListControl micListControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.inputLegend)]
    private VisualElement inputLegend;

    [Inject(UxmlName = R.UxmlNames.inputDeviceIcon)]
    private VisualElement inputDeviceIcon;

    [Inject(UxmlName = R.UxmlNames.menuOverlayInputLegend)]
    private VisualElement menuOverlayInputLegend;

    [Inject(UxmlName = R.UxmlNames.songIndexLabel)]
    private Label songIndexLabel;

    [Inject(UxmlName = R.UxmlNames.songIndexButton)]
    private Button songIndexButton;

    [Inject(UxmlName = R.UxmlNames.durationLabel)]
    private Label durationLabel;

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
    private VisualElement duetIcon;

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

    [Inject(UxmlName = R.UxmlNames.menuButton)]
    private Button menuButton;

    [Inject(UxmlName = R.UxmlNames.menuOverlay)]
    private VisualElement menuOverlay;

    [Inject(UxmlName = R.UxmlNames.closeMenuOverlayButton)]
    private Button closeMenuOverlayButton;

    [Inject(UxmlName = R.UxmlNames.songDetailOverlay)]
    private VisualElement songDetailOverlay;

    [Inject(UxmlName = R.UxmlNames.toggleSongDetailOverlayButton)]
    private Button toggleSongDetailOverlayButton;

    [Inject(UxmlName = R.UxmlNames.songDetailOverlayScrollView)]
    private VisualElement songDetailOverlayScrollView;

    [Inject(UxmlName = R.UxmlNames.backToMainMenuButton)]
    private Button backToMainMenuButton;

    [Inject(UxmlName = R.UxmlNames.nextSongButton)]
    private Button nextSongButton;

    [Inject(UxmlName = R.UxmlNames.previousSongButton)]
    private Button previousSongButton;

    [Inject(UxmlName = R.UxmlNames.duetLegendLabel)]
    private Label duetLegendLabel;

    [Inject(UxmlName = R.UxmlNames.videoLegendLabel)]
    private Label videoLegendLabel;

    [Inject(UxmlName = R.UxmlNames.songOrderPicker)]
    private ItemPicker songOrderItemPicker;

    public SongOrderPickerControl SongOrderPickerControl { get; private set; }

    private SongSelectSceneData sceneData;
    private List<SongMeta> songMetas;
    private int lastSongMetasReloadFrame = -1;
    private string lastRawSearchText;
    private SongMeta selectedSongBeforeSearch;

    [Inject]
    private Statistics statistics;

    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundLabel)]
    private VisualElement noSongsFoundLabel;

    public PlaylistChooserControl PlaylistChooserControl { get; private set; }

    public bool IsPlayerSelectOverlayVisible => playerSelectOverlayContainer.IsVisibleByDisplay();
    public bool IsMenuOverlayVisible => menuOverlay.IsVisibleByDisplay();
    public bool IsSongDetailOverlayVisible => songDetailOverlay.IsVisibleByDisplay();

    private SongSearchControl songSearchControl;
    public SongSearchControl SongSearchControl
    {
        get
        {
            if (songSearchControl == null)
            {
                songSearchControl = new SongSearchControl();
                injector.Inject(songSearchControl);
            }
            return songSearchControl;
        }
    }

    public SongMeta SelectedSong
    {
        get
        {
            return songRouletteControl.Selection.Value.SongMeta;
        }
    }

    public int SelectedSongIndex
    {
        get
        {
            return songRouletteControl.Songs.IndexOf(SelectedSong);
        }
    }

    private void Start()
    {
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!SongMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        sceneData = SceneNavigator.Instance.GetSceneData(CreateDefaultSceneData());

        InitSongMetas();

        HidePlayerSelectOverlay();
        HideMenuOverlay();
        HideSongDetailOverlay();

        SongOrderPickerControl = new SongOrderPickerControl(songOrderItemPicker);

        // Register Callbacks
        toggleFavoriteButton.RegisterCallbackButtonTriggered(() => ToggleSelectedSongIsFavorite());

        closePlayerSelectOverlayButton.RegisterCallbackButtonTriggered(() => HidePlayerSelectOverlay());

        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.text = newValue);

        startButton.RegisterCallbackButtonTriggered(() => CheckAudioAndStartSingScene());

        menuButton.RegisterCallbackButtonTriggered(() => ShowMenuOverlay());
        closeMenuOverlayButton.RegisterCallbackButtonTriggered(() => HideMenuOverlay());
        backToMainMenuButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.MainScene));

        toggleSongDetailOverlayButton.RegisterCallbackButtonTriggered(() =>
        {
            if (IsSongDetailOverlayVisible)
            {
                HideSongDetailOverlay();
            }
            else
            {
                ShowSongDetailOverlay();
            }
        });

        nextSongButton.RegisterCallbackButtonTriggered(() => songRouletteControl.SelectNextSong());
        previousSongButton.RegisterCallbackButtonTriggered(() => songRouletteControl.SelectPreviousSong());
        UpdateNextAndPreviousSongButtonLabels();
        inputManager.InputDeviceChangeEventStream.Subscribe(evt => UpdateNextAndPreviousSongButtonLabels());

        songIndexButton.RegisterCallbackButtonTriggered(() => songSearchControl.SetSearchText($"#{SelectedSongIndex + 1}"));

        SongSearchControl.SearchChangedEventStream.Subscribe(_ => OnSearchTextChanged());

        PlaylistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());

        SongOrderPickerControl.Selection.Subscribe(newValue =>
        {
            settings.SongSelectSettings.songOrder = newValue;
            characterQuickJumpListControl.UpdateCharacters();
            UpdateFilteredSongs();
        });

        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == PlaylistChooserControl.Selection.Value)
            {
                UpdateFilteredSongs();
            }

            UpdateFavoriteIcon();
        });

        InitSongRouletteSongMetas();
        songRouletteControl.SelectionClickedEventStream
            .Subscribe(_ => CheckAudioAndStartSingScene());

        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        focusableNavigator.FocusSongRoulette();

        songAudioPlayer.AudioClipLoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationOfSongInMillis));
    }

    private void UpdateNextAndPreviousSongButtonLabels()
    {
        if (inputManager.InputDeviceEnum == EInputDevice.Gamepad)
        {
            nextSongButton.text = "R1 >";
            previousSongButton.text = "< L1";
        }
        else
        {
            nextSongButton.text = ">";
            previousSongButton.text = "<";
        }
    }

    public void ShowMenuOverlay()
    {
        menuOverlay.ShowByDisplay();
        closeMenuOverlayButton.Focus();
    }

    public void HideMenuOverlay()
    {
        menuOverlay.HideByDisplay();
        menuButton.Focus();
    }

    public void ShowSongDetailOverlay()
    {
        songDetailOverlay.ShowByDisplay();
        UpdateSongDetailsInOverlay();
    }

    public void HideSongDetailOverlay()
    {
        songDetailOverlay.HideByDisplay();
    }

    private void UpdateFavoriteIcon()
    {
        VectorImage vectorImage = IsFavorite(SelectedSong)
            ? favoriteImageAsset
            : noFavoriteImageAsset;
        favoriteIcon.style.backgroundImage = new StyleBackground(vectorImage);
    }

    public void InitSongMetas()
    {
        songMetas = new List<SongMeta>(SongMetaManager.Instance.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        noSongsFoundLabel.SetVisibleByDisplay(songMetas.IsNullOrEmpty());
    }

    private void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (songMetas.Count != SongMetaManager.Instance.GetSongMetas().Count
            && lastSongMetasReloadFrame + 10 < Time.frameCount)
        {
            InitSongMetas();
            SongMeta selectedSong = songRouletteControl.Selection.Value.SongMeta;
            InitSongRouletteSongMetas();
            songRouletteControl.SelectSong(selectedSong);
        }
    }

    private void InitSongRouletteSongMetas()
    {
        lastSongMetasReloadFrame = Time.frameCount;
        UpdateFilteredSongs();
        if (sceneData.SongMeta != null)
        {
            songRouletteControl.SelectSong(sceneData.SongMeta);
        }

        songRouletteControl.Selection.Subscribe(newValue => OnSongSelectionChanged(newValue));
    }

    private void OnSongSelectionChanged(SongSelection selection)
    {
        songRouletteControl.HideSongMenuOverlay();

        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            songIndexLabel.text = "-";
            return;
        }

        genreLabel.text = selectedSong.Genre;
        yearLabel.text = selectedSong.Year > 0
            ? selectedSong.Year.ToString()
            : "";
        songIndexLabel.text = (selection.SongIndex + 1) + "\nof " + selection.SongsCount;

        // The song duration requires loading the audio file.
        // Loading every song only to show its duration is slow (e.g. when scrolling through songs).
        // Instead, the label is updated when the AudioClip has been loaded.
        durationLabel.text = "";

        bool hasVideo = !string.IsNullOrEmpty(selectedSong.Video);
        videoIndicator.SetVisibleByVisibility(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIcon.SetVisibleByVisibility(isDuet);

        UpdateFavoriteIcon();

        UpdateSongStatistics(selectedSong);

        if (IsSongDetailOverlayVisible)
        {
            UpdateSongDetailsInOverlay();
        }
    }

    private void UpdateSongDurationLabel(float durationInMillis)
    {
        int min = (int)Math.Floor(durationInMillis / 1000 / 60);
        int seconds = (int)Math.Floor((durationInMillis / 1000) % 60);
        durationLabel.text = $"{min}:{seconds.ToString().PadLeft(2, '0')}";
    }

    private void UpdateSongStatistics(SongMeta songMeta)
    {
        LocalStatistic localStatistic = statistics.GetLocalStats(songMeta);
        if (localStatistic != null)
        {
            timesClearedLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_timesClearedInfo,
                "value", localStatistic.TimesFinished);
            timesCanceledLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_timesCanceledInfo,
                "value", localStatistic.TimesCanceled);

            List<SongStatistic> topScores = localStatistic.StatsEntries.GetTopScores(3);
            List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();

            UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);
            UpdateTopScoreLabels(new List<int>(), onlineHighScoreContainer);
        }
        else
        {
            timesClearedLabel.text = "";
            timesCanceledLabel.text = "";

            UpdateTopScoreLabels(new List<int>(), localHighScoreContainer);
            UpdateTopScoreLabels(new List<int>(), onlineHighScoreContainer);
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
        SingSceneData singSceneData = new();
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
        SongEditorSceneData editorSceneData = new();
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
        return new SongSelectSceneData();
    }

    private void SetEmptySongDetails()
    {
        genreLabel.text = "";
        yearLabel.text = "";
        timesClearedLabel.text = "";
        timesCanceledLabel.text = "";
        videoIndicator.HideByVisibility();
        duetIcon.HideByVisibility();
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
            if (!WebRequestUtils.IsHttpOrHttpsUri(SelectedSong.Mp3))
            {
                string audioUri = SongMetaUtils.GetAudioUri(SelectedSong);
                if (!WebRequestUtils.ResourceExists(audioUri))
                {
                    string message = "Audio file resource does not exist: " + audioUri;
                    Debug.Log(message);
                    uiManager.CreateNotificationVisualElement(message);
                    return;
                }
            }

            // Check that the used audio format can be loaded.
            songAudioPlayer.Init(SelectedSong);
            if (!songAudioPlayer.HasAudioClip)
            {
                string message = $"Audio file '{SelectedSong.Mp3}' could not be loaded.\nPlease use a supported format.";
                Debug.Log(message);
                uiManager.CreateNotificationVisualElement(message);
                return;
            }

            ShowPlayerSelectOverlay();
        }
    }

    private void ShowPlayerSelectOverlay()
    {
        playerSelectOverlayContainer.ShowByDisplay();
        UpdateInputLegend();

        // Show lyrics for duet song
        bool hasMultipleVoices = SelectedSong.VoiceNames.Count > 1;
        leftLyricsOverlay.SetVisibleByDisplay(hasMultipleVoices);
        rightLyricsOverlay.SetVisibleByDisplay(hasMultipleVoices);
        if (hasMultipleVoices)
        {
            List<string> voiceNames = SelectedSong.VoiceNames.Values.ToList();
            leftLyricsOverlay.Q<Label>(R.UxmlNames.voiceNameLabel).text = voiceNames[0];
            leftLyricsOverlay.Q<Label>(R.UxmlNames.lyricsLabel).text = SongMetaUtils.GetLyrics(SelectedSong, Voice.firstVoiceName);

            rightLyricsOverlay.Q<Label>(R.UxmlNames.voiceNameLabel).text = voiceNames[1];
            rightLyricsOverlay.Q<Label>(R.UxmlNames.lyricsLabel).text = SongMetaUtils.GetLyrics(SelectedSong, Voice.secondVoiceName);

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
        UpdateInputLegend();
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
        string rawSearchText = songSearchControl.GetRawSearchText();

        if (lastRawSearchText.IsNullOrEmpty()
            && !rawSearchText.IsNullOrEmpty())
        {
            selectedSongBeforeSearch = SelectedSong;
        }
        lastRawSearchText = rawSearchText;

        if (TryExecuteSpecialSearchSyntax(rawSearchText))
        {
            // Special search syntax used. Do not perform normal filtering.
            return;
        }

        UpdateFilteredSongs();
        if (string.IsNullOrEmpty(songSearchControl.GetSearchText()))
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
        // Ignore prefix for special search syntax
        UltraStarPlaylist playlist = PlaylistChooserControl.Selection.Value;
        List<SongMeta> filteredSongs = songSearchControl.GetFilteredSongMetas(songMetas)
            .Where(songMeta => playlist == null
                            || playlist.HasSongEntry(songMeta.Artist, songMeta.Title))
            .OrderBy(songMeta => GetSongMetaOrderByProperty(songMeta))
            .ToList();
        return filteredSongs;
    }

    private object GetSongMetaOrderByProperty(SongMeta songMeta)
    {
        switch (SongOrderPickerControl.SelectedItem)
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
            case ESongOrder.CountCanceled:
                return statistics.GetLocalStats(songMeta)?.TimesCanceled;
            case ESongOrder.CountFinished:
                return statistics.GetLocalStats(songMeta)?.TimesFinished;
            default:
                Debug.LogWarning("Unknown order for songs: " + SongOrderPickerControl.SelectedItem);
                return songMeta.Artist;
        }
    }

    public void ToggleSelectedPlayers()
    {
        playerListControl.ToggleSelectedPlayers();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(songRouletteControl);
        bb.BindExistingInstance(songSelectSceneInputControl);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(SongOrderPickerControl);
        bb.BindExistingInstance(characterQuickJumpListControl);
        bb.BindExistingInstance(playerListControl);
        bb.BindExistingInstance(focusableNavigator);
        bb.Bind(typeof(FocusableNavigator)).ToExistingInstance(focusableNavigator);
        bb.BindExistingInstance(songPreviewControl);
        return bb.GetBindings();
    }

    public void ToggleFavoritePlaylist()
    {
        PlaylistChooserControl.ToggleFavoritePlaylist();
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
    }

    public bool IsPlaylistActive()
    {
        return PlaylistChooserControl.Selection.Value != null
               && !(PlaylistChooserControl.Selection.Value is UltraStarAllSongsPlaylist);
    }

    public void ResetPlaylistSelection()
    {
        PlaylistChooserControl.Reset();
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
                return true;
            }
        }
        return false;
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && sceneTitle == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_title);

        menuButton.text = TranslationManager.GetTranslation(R.Messages.menu);
        closeMenuOverlayButton.text = TranslationManager.GetTranslation(R.Messages.back);
        backToMainMenuButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_title);
        toggleSongDetailOverlayButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_toggleSongDetailsButton);
        duetLegendLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_duetLegendLabel);
        videoLegendLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_videoLegendLabel);
        closePlayerSelectOverlayButton.text = TranslationManager.GetTranslation(R.Messages.back);
        startButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_label);

        localHighScoreContainer.Q<Label>(R.UxmlNames.title).text = TranslationManager.GetTranslation(R.Messages.songSelectScene_localTopScoresTitle);
        onlineHighScoreContainer.Q<Label>(R.UxmlNames.title).text = TranslationManager.GetTranslation(R.Messages.songSelectScene_onlineTopScoresTitle);

        PlaylistChooserControl.UpdateTranslation();
        SongSearchControl.UpdateTranslation();
        songRouletteControl.UpdateTranslation();
        UpdateInputLegend();
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
               && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta.Artist, songMeta.Title);
    }

    public void SubmitSearch()
    {
        selectedSongBeforeSearch = SelectedSong;
        songSearchControl.ResetSearchText();
    }

    public void OnInjectionFinished()
    {
        PlaylistChooserControl = new PlaylistChooserControl();
        injector.Inject(PlaylistChooserControl);
    }

    private void UpdateInputLegend()
    {
        inputLegend.Query<Label>()
            .ForEach(label => label.RemoveFromHierarchy());

        if (IsPlayerSelectOverlayVisible)
        {
            InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
                TranslationManager.GetTranslation(R.Messages.back),
                inputLegend);
            InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_togglePlayers,
                TranslationManager.GetTranslation(R.Messages.action_togglePlayers),
                inputLegend);
        }
        else
        {
            InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
                TranslationManager.GetTranslation(R.Messages.back),
                inputLegend);
            InputLegendControl.TryAddInputActionInfo(R.InputActions.ui_submit,
                TranslationManager.GetTranslation(R.Messages.submit),
                inputLegend);
            InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_toggleSongMenu,
                TranslationManager.GetTranslation(R.Messages.action_openSongMenu),
                inputLegend);
        }
        if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        {
            inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
                TranslationManager.GetTranslation(R.Messages.action_openSongMenu),
                TranslationManager.GetTranslation(R.Messages.action_longPress))));
        }

        menuOverlayInputLegend.Clear();
        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_randomSong,
            TranslationManager.GetTranslation(R.Messages.action_randomSong),
            menuOverlayInputLegend);
        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_openSongEditor,
            TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            menuOverlayInputLegend);
        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_toggleFavorite,
            TranslationManager.GetTranslation(R.Messages.action_toggleFavorites),
            menuOverlayInputLegend);
    }

    private void UpdateSongDetailsInOverlay()
    {
        songDetailOverlayScrollView.Clear();
        SongMeta songMeta = SelectedSong;
        if (songMeta == null)
        {
            return;
        }

        Label CreateSongDetailLabel(string fieldName, object fieldValue)
        {
            Label label = new();
            label.enableRichText = true;
            label.AddToClassList("songDetailOverlayLabel");
            string fieldValueDisplayString = fieldValue?.ToString();
            fieldValueDisplayString = fieldValueDisplayString.IsNullOrEmpty()
                ? "-"
                : fieldValueDisplayString;
            label.text = $"<b><u>{fieldName}</u></b>: {fieldValueDisplayString}";
            return label;
        }

        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Artist", songMeta.Artist));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Title", songMeta.Title));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Genre", songMeta.Genre));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Year", songMeta.Year > 0 ? songMeta.Year : null));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Audio", songMeta.Mp3));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Gap", songMeta.Gap));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Bpm", songMeta.Bpm));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Video", songMeta.Video));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Video Gap", songMeta.VideoGap));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Cover", songMeta.Cover));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Language", songMeta.Language));
        songDetailOverlayScrollView.Add(CreateSongDetailLabel("Edition", songMeta.Edition));
        songDetailOverlayScrollView.Add(new Label("\n"));

        songMeta.VoiceNames.Keys.ForEach(voiceNameKey =>
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, voiceNameKey);
            if (lyrics.IsNullOrEmpty())
            {
                return;
            }

            if (voiceNameKey != Voice.soloVoiceName)
            {
                Label voiceNameLabel = new();
                voiceNameLabel.enableRichText = true;
                voiceNameLabel.AddToClassList("songDetailOverlayLabel");
                string voiceName = songMeta.VoiceNames[voiceNameKey];
                string voiceNameText = voiceName != voiceNameKey
                    ? $" ({voiceName})"
                    : "";
                voiceNameLabel.text = $"<b>{voiceNameKey}{voiceNameText}</b>";
                songDetailOverlayScrollView.Add(voiceNameLabel);
            }

            Label lyricsLabel = new();
            lyricsLabel.AddToClassList("songDetailOverlayLabel");
            lyricsLabel.text = lyrics;

            songDetailOverlayScrollView.Add(lyricsLabel);
            songDetailOverlayScrollView.Add(new Label("\n"));
        });
    }
}
