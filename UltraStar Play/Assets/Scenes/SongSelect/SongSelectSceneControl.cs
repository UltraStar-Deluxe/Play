using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public SongSelectSongPreviewControl songPreviewControl;

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

    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private MaterialIcon noFavoriteIcon;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private MaterialIcon favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.localHighScoreContainer)]
    private VisualElement localHighScoreContainer;

    [Inject(UxmlName = R.UxmlNames.onlineHighScoreContainer)]
    private VisualElement onlineHighScoreContainer;

    [Inject(UxmlName = R.UxmlNames.toggleFavoriteButton)]
    private Button toggleFavoriteButton;

    [Inject(UxmlName = R.UxmlNames.fuzzySearchTextLabel)]
    private Label fuzzySearchTextLabel;

    [Inject(UxmlName = R.UxmlNames.songSelectPlayerSelectUi)]
    private VisualElement songSelectPlayerSelectUi;

    [Inject(UxmlName = R.UxmlNames.playerSelectOverlayContainer)]
    private VisualElement playerSelectOverlayContainer;

    [Inject(UxmlName = R.UxmlNames.closePlayerSelectOverlayButton)]
    private Button closePlayerSelectOverlayButton;

    [Inject(UxmlName = R.UxmlNames.leftLyricsOverlay)]
    private VisualElement leftLyricsOverlay;

    [Inject(UxmlName = R.UxmlNames.rightLyricsOverlay)]
    private VisualElement rightLyricsOverlay;

    [Inject(UxmlName = R.UxmlNames.playerSelectStartSongButton)]
    private Button playerSelectStartSongButton;

    [Inject(UxmlName = R.UxmlNames.playerSelectOpenSongEditorButton)]
    private Button playerSelectOpenSongEditorButton;

    [Inject(UxmlName = R.UxmlNames.playerSelectCreateSongButton)]
    private Button playerSelectCreateSongButton;

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

    [Inject(UxmlName = R.UxmlNames.quitSongSelectButton)]
    private Button quitSongSelectButton;

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

    [Inject(UxmlName = R.UxmlNames.scoreModeLabel)]
    private Label scoreModeLabel;

    [Inject(UxmlName = R.UxmlNames.scoreModePicker)]
    private ItemPicker scoreModePicker;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModeLabel)]
    private Label noteDisplayModeLabel;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModePicker)]
    private ItemPicker noteDisplayModePicker;

    [Inject(UxmlName = R.UxmlNames.micListOverlay)]
    private VisualElement micListOverlay;

    [Inject(UxmlName = R.UxmlNames.singingOptionsScrollView)]
    private VisualElement singingOptionsScrollView;

    [Inject(UxmlName = R.UxmlNames.toggleSingingOptionsButton)]
    private Button toggleSingingOptionsButton;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsNewButton)]
    private Button addToSongQueueAsNewButton;
    
    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsMedleyButton)]
    private Button addToSongQueueAsMedleyButton;
    
    [Inject(UxmlName = R.UxmlNames.playerScrollView)]
    private VisualElement playerScrollView;

    public SongOrderPickerControl SongOrderPickerControl { get; private set; }

    [Inject]
    private SongSelectSceneData sceneData;

    private List<SongMeta> songMetas;
    private int lastSongMetasReloadFrame = -1;
    private string lastRawSearchText;
    private SongMeta selectedSongBeforeSearch;

    [Inject]
    private Statistics statistics;

    [Inject(Optional = true)]
    private EventSystem eventSystem;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;
    
    [Inject]
    private SongQueueManager songQueueManager;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundLabel)]
    private Label noSongsFoundLabel;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundContainer)]
    private VisualElement noSongsFoundContainer;

    [Inject(UxmlName = R.UxmlNames.downloadSongsButton)]
    private Button downloadSongsButton;

    [Inject(UxmlName = R.UxmlNames.addSongFolderButton)]
    private Button addSongFolderButton;

    [Inject(UxmlName = R.UxmlNames.selectRandomSongButton)]
    private Button selectRandomSongButton;

    [Inject(UxmlName = R.UxmlNames.toggleSongQueueOverlayButton)]
    private Button toggleSongQueueOverlayButton;
    
    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;

    public SongSelectionPlaylistChooserControl SongSelectionPlaylistChooserControl { get; private set; } = new();

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

    private int SelectedSongIndex
    {
        get
        {
            return songRouletteControl.Songs.IndexOf(SelectedSong);
        }
    }

    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSettings PartyModeSettings => PartyModeSceneData.PartyModeSettings;
    public bool IsPartyModeRandomSongSelection => HasPartyModeSceneData
                                                  && PartyModeSettings.songSelectionSettings.songSelectionMode == EPartyModeSongSelectionMode.Random;
    public bool UsePartyModePlaylist => IsPartyModeRandomSongSelection
                                        && PartyModeSettings.songSelectionSettings.songPoolPlaylist != null;
    public bool CanUseSongSelectionJoker => PartyModeSceneData.remainingJokerCount != 0;

    private readonly CreateSingAlongSongControl createSingAlongSongControl = new();
    private readonly SongSelectScenePartyModeControl partyModeControl = new();
    private readonly GameRoundSettingsUiControl gameRoundSettingsUiControl = new();
    private readonly SongQueueUiControl songQueueUiControl = new();
    private readonly SongSelectFilterControl songSelectFilterControl = new();

    public void OnInjectionFinished()
    {
        injector.Inject(SongSelectionPlaylistChooserControl);
        injector.Inject(createSingAlongSongControl);
        injector.Inject(partyModeControl);
        injector.Inject(songQueueUiControl);
        injector.Inject(songSelectFilterControl);
    }
    
    private void Start()
    {
        songMetaManager.ScanFilesIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!SongMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        SongOrderPickerControl = new SongOrderPickerControl(songOrderItemPicker);
        
        InitSongMetas();

        if (HasPartyModeSceneData
            && PartyModeSettings.songSelectionSettings.songSelectionMode == EPartyModeSongSelectionMode.Random)
        {
            partyModeControl.SelectRandomSong();
        }

        InitModifiersChipsComboControl();

        HidePlayerSelectOverlay();
        HideMenuOverlay();
        HideSongDetailOverlay();
        
        // Register Callbacks
        toggleFavoriteButton.RegisterCallbackButtonTriggered(() => ToggleSelectedSongIsFavorite());
        selectRandomSongButton.RegisterCallbackButtonTriggered(() => SelectRandomSong());

        closePlayerSelectOverlayButton.RegisterCallbackButtonTriggered(() => HidePlayerSelectOverlay());

        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.text = newValue);

        playerSelectStartSongButton.RegisterCallbackButtonTriggered(() => AttemptStartSong());
        playerSelectCreateSongButton.RegisterCallbackButtonTriggered(() => createSingAlongSongControl.CreateSingAlongSong(SelectedSong));
        playerSelectOpenSongEditorButton.RegisterCallbackButtonTriggered(() => StartSongEditorScene());
        if (HasPartyModeSceneData)
        {
            playerSelectOpenSongEditorButton.SetEnabled(false);
        }

        menuButton.RegisterCallbackButtonTriggered(() => ShowMenuOverlay());
        closeMenuOverlayButton.RegisterCallbackButtonTriggered(() => HideMenuOverlay());
        quitSongSelectButton.RegisterCallbackButtonTriggered(() => QuitSongSelect());

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

        SongSearchControl.SearchChangedEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => OnSearchTextChanged());

        SongSelectionPlaylistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());
        songSelectFilterControl.FiltersChangedEventStream.Subscribe(_ => UpdateFilteredSongs());

        SongOrderPickerControl.Selection.Subscribe(newValue =>
        {
            settings.SongSelectSettings.songOrder = newValue;
            characterQuickJumpListControl.UpdateCharacters();
            UpdateFilteredSongs();
        });

        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == SongSelectionPlaylistChooserControl.Selection.Value)
            {
                UpdateFilteredSongs();
            }

            UpdateFavoriteIcon();
        });

        InitSongRouletteSongMetas();
        songRouletteControl.SelectionClickedEventStream
            .Subscribe(_ => AttemptStartSong());

        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        focusableNavigator.FocusSongRoulette();

        songAudioPlayer.AudioClipLoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationOfSongInMillis));

        downloadSongsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ContentDownloadScene));
        addSongFolderButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.SongLibraryOptionsScene));

        // Toggle player select and singing options container
        ShowPlayerSelectContainer();
        toggleSingingOptionsButton.RegisterCallbackButtonTriggered(() => TogglePlayerSelectAndSingingOptions());

        // Init singing options
        new ScoreModeItemPickerControl(scoreModePicker)
            .Bind(() => settings.GameSettings.ScoreMode,
                newValue => settings.GameSettings.ScoreMode = newValue);
        new NoteDisplayModeItemPickerControl(noteDisplayModePicker)
            .Bind(() => settings.GraphicSettings.noteDisplayMode,
                newValue => settings.GraphicSettings.noteDisplayMode = newValue);

        createSingAlongSongControl.CreatedSingAlongVersionEventStream.Subscribe(processedSongMeta =>
        {
            UiManager.CreateNotification($"Created sing-along version of '{Path.GetFileName(processedSongMeta.Mp3)}'");
            UpdatePlayerSelectOverlayButtons();
        });

        // Close overlays by clicking none of its child elements
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(playerSelectOverlayContainer, HidePlayerSelectOverlay);
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(menuOverlay, HideMenuOverlay);

        songQueueManager.SongQueueChangedEventStream
            .Subscribe(_ => songQueueUiControl.SetSongQueueEntryDtos(songQueueManager.GetSongQueueEntries()));

        toggleSongQueueOverlayButton.RegisterCallbackButtonTriggered(() =>
        {
            songQueueOverlay.ToggleVisibleByDisplay();
        });
        addToSongQueueAsNewButton.RegisterCallbackButtonTriggered(() => AddCurrentSongToSongQueue());
        addToSongQueueAsMedleyButton.RegisterCallbackButtonTriggered(() => AddCurrentSongToSongQueueAsMedley());
        songQueueUiControl.OnToggleMedley = songQueueEntryDto => songQueueManager.ToggleMedley(songQueueEntryDto);
        songQueueUiControl.OnDelete = songQueueEntryDto => songQueueManager.RemoveSongQueueEntry(songQueueEntryDto);
    }

    private void InitModifiersChipsComboControl()
    {
        GameRoundSettings gameRoundSettings = HasPartyModeSceneData
            ? PartyModeSceneData.CurrentRoundSettings
            : settings.GameRoundSettings;

        injector.Inject(gameRoundSettingsUiControl);
        gameRoundSettingsUiControl.GameRoundSettings = gameRoundSettings;
    }

    public void QuitSongSelect()
    {
        if (HasPartyModeSceneData)
        {
            PartyModeSceneData partyModeSceneData = new();
            partyModeSceneData.PartyModeSettings = PartyModeSettings;
            sceneNavigator.LoadScene(EScene.PartyModeScene, partyModeSceneData);
        }
        else
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
    }
    
    private void AddCurrentSongToSongQueue()
    {
        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings();
        songQueueManager.AddSongQueueEntry(songQueueEntryDto);
    }

    private void AddCurrentSongToSongQueueAsMedley()
    {
        if (HasPartyModeSceneData)
        {
            // Medleys not supported in party mode
            return;
        }
        
        if (songQueueManager.IsSongQueueEmpty)
        {
            // Cannot create medley with previous song when song queue is empty.
            return;
        }
        
        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings();
        if (songQueueEntryDto != null)
        {
            songQueueEntryDto.IsMedleyWithPreviousEntry = true;
            songQueueManager.AddSongQueueEntry(songQueueEntryDto);
        }
    }

    private SongQueueEntryDto CreateSongQueueEntryWithCurrentSettings()
    {
        SongQueueEntryDto songQueueEntryDto = new();
        songQueueEntryDto.SongDto = DtoConverter.ToDto(SelectedSong);
        songQueueEntryDto.SingScenePlayerDataDto = DtoConverter.ToDto(CreateSingScenePlayerData());
        songQueueEntryDto.GameRoundSettings = new(settings.GameRoundSettings);
        return songQueueEntryDto;
    }

    private void ShowPlayerSelectContainer()
    {
        playerScrollView.ShowByDisplay();
        micListOverlay.ShowByDisplay();
        singingOptionsScrollView.HideByDisplay();
        toggleSingingOptionsButton.Q<VisualElement>(R.UxmlNames.settingsIcon).ShowByDisplay();
        toggleSingingOptionsButton.Q<VisualElement>(R.UxmlNames.playersIcon).HideByDisplay();
    }

    private void ShowSingingOptionsContainer()
    {
        playerScrollView.HideByDisplay();
        micListOverlay.HideByDisplay();
        singingOptionsScrollView.ShowByDisplay();
        toggleSingingOptionsButton.Q<VisualElement>(R.UxmlNames.settingsIcon).HideByDisplay();
        toggleSingingOptionsButton.Q<VisualElement>(R.UxmlNames.playersIcon).ShowByDisplay();

    }

    private void TogglePlayerSelectAndSingingOptions()
    {
        if (playerScrollView.IsVisibleByDisplay())
        {
            ShowSingingOptionsContainer();
        }
        else
        {
            ShowPlayerSelectContainer();
        }
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
        bool isFavorite = IsFavorite(SelectedSong);
        favoriteIcon.SetVisibleByDisplay(isFavorite);
        noFavoriteIcon.SetVisibleByDisplay(!isFavorite);
    }

    public void InitSongMetas()
    {
        songMetas = new List<SongMeta>(songMetaManager.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        noSongsFoundLabel.SetVisibleByDisplay(songMetas.IsNullOrEmpty());
        noSongsFoundContainer.SetVisibleByDisplay(songMetas.IsNullOrEmpty());
    }

    private void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (songMetas.Count != songMetaManager.GetSongMetas().Count
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

        bool hasVideo = !selectedSong.Video.IsNullOrEmpty();
        videoIndicator.SetVisibleByVisibility(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIcon.SetVisibleByVisibility(isDuet);

        UpdateFavoriteIcon();

        UpdateSongStatistics(selectedSong);

        UpdatePlayerSelectOverlayButtons();
        UpdateModifiersChipsCombo();

        if (IsSongDetailOverlayVisible)
        {
            UpdateSongDetailsInOverlay();
        }
    }

    private void UpdateModifiersChipsCombo()
    {
        
    }

    private void UpdatePlayerSelectOverlayButtons()
    {
        playerSelectStartSongButton.SetVisibleByDisplay(SongMetaUtils.SongMetaFileExists(SelectedSong));
        playerSelectCreateSongButton.SetVisibleByDisplay(!SongMetaUtils.SongMetaFileExists(SelectedSong));
    }

    private void UpdateSongDurationLabel(double durationInMillis)
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

    private SingSceneData CreateSingSceneDataWithSelectedSongAndSettings()
    {
        SingSceneData singSceneData = new();
        singSceneData.SongMetas = new List<SongMeta> { SelectedSong };
        singSceneData.SingScenePlayerData = CreateSingScenePlayerData();
        singSceneData.partyModeSceneData = sceneData.partyModeSceneData;

        if (HasPartyModeSceneData)
        {
            singSceneData.gameRoundSettings = new(PartyModeSceneData.CurrentRoundSettings);
        }
        else
        {
            singSceneData.gameRoundSettings = new(settings.GameRoundSettings);
        }
        
        if (singSceneData.gameRoundSettings != null
            && singSceneData.gameRoundSettings.modifiers.Contains(EGameRoundModifier.ShortSong))
        {
            // Set as medley song to play shortened version
            singSceneData.MedleySongIndex = 0;
        }
        return singSceneData;
    }

    private SingScenePlayerData CreateSingScenePlayerData()
    {
        SingScenePlayerData singScenePlayerData = new();

        List<PlayerProfile> selectedPlayerProfiles = playerListControl.GetSelectedPlayerProfiles();
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            UiManager.CreateNotification(TranslationManager.GetTranslation(R.Messages.songSelectScene_noPlayerSelected_title));
            return null;
        }
        singScenePlayerData.SelectedPlayerProfiles = selectedPlayerProfiles;
        singScenePlayerData.PlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();
        singScenePlayerData.PlayerProfileToVoiceNameMap = playerListControl.GetSelectedPlayerProfileToVoiceNameMap();
        return singScenePlayerData;
    }

    private void StartSingScene()
    {
        if (!songQueueManager.IsSongQueueEmpty
            && !HasPartyModeSceneData)
        {
            StartSingSceneWithNextSongQueueEntry();
        }
        else
        {
            StartSingSceneWithSelectedSongAndSettings();
        }
    }

    private void StartSingSceneWithNextSongQueueEntry()
    {
        songQueueManager.StartNextEntry();
    }

    private void StartSingSceneWithSelectedSongAndSettings()
    {
        if (SelectedSong.FailedToLoadVoices)
        {
            UiManager.CreateNotification("Failed to load song. Check log for details.");
            return;
        }

        SingSceneData singSceneData = CreateSingSceneDataWithSelectedSongAndSettings();
        if (singSceneData != null)
        {
            sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
        }
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        if (HasPartyModeSceneData)
        {
            UiManager.CreateNotification("Song editor not available in party mode");
            return;
        }

        if (songMeta.FailedToLoadVoices)
        {
            UiManager.CreateNotification("Failed to load song. Check log for details.");
            return;
        }

        SongEditorSceneData editorSceneData = new();
        editorSceneData.SongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneDataWithSelectedSongAndSettings();
        if (singSceneData != null)
        {
            editorSceneData.PlayerProfileToMicProfileMap = singSceneData.SingScenePlayerData.PlayerProfileToMicProfileMap;
            editorSceneData.SelectedPlayerProfiles = singSceneData.SingScenePlayerData.SelectedPlayerProfiles;
        }
        editorSceneData.PreviousSceneData = sceneData;
        editorSceneData.PreviousScene = EScene.SongSelectScene;

        sceneNavigator.LoadScene(EScene.SongEditorScene, editorSceneData);
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

    public void SelectRandomSong()
    {
        SongMeta randomSongMeta = RandomUtils.RandomOf(songRouletteControl.Songs);
        songRouletteControl.SelectSong(randomSongMeta);
    }

    private void CheckAudioAndShowPlayerSelectOverlay()
    {
        if (SelectedSong == null)
        {
            return;
        }

        // Check that the audio file exists
        if (!SongMetaUtils.AudioResourceExists(SelectedSong))
        {
            string audioUri = SongMetaUtils.GetAudioUri(SelectedSong);
            string message = "Audio file resource does not exist: " + audioUri;
            Debug.Log(message);
            UiManager.CreateNotification(message);
            return;
        }

        // Check that the used audio format can be loaded.
        songAudioPlayer.Init(SelectedSong);
        if (!songAudioPlayer.HasAudioClip)
        {
            string message = $"Audio file '{SelectedSong.Mp3}' could not be loaded.\nPlease use a supported format.";
            Debug.Log(message);
            UiManager.CreateNotification(message);
            return;
        }

        // Start the sing scene or show the player select overlay.
        if (playerSelectOverlayContainer.IsVisibleByDisplay()
            && !SongMetaUtils.IsGeneratedAndNotYetSaved(SelectedSong))
        {
            StartSingScene();
        }
        else
        {
            ShowPlayerSelectOverlay();
        }
    }

    public void AttemptStartSong()
    {
        if (IsPartyModeRandomSongSelection
            && partyModeControl.RandomlySelectedSong != songRouletteControl.SelectedSongEntryControl.SongMeta)
        {
            // The user selected a different song than the randomly selected.
            // Ask to use joker or quit.
            if (CanUseSongSelectionJoker)
            {
                partyModeControl.OpenAskToUseJokerDialog(
                    songRouletteControl.SelectedSongEntryControl.SongMeta,
                    () => CheckAudioAndShowPlayerSelectOverlay());
            }
            else
            {
                // No jokers left, go back to randomly selected song
                ShowCannotUseJokerMessage();
                songRouletteControl.SelectSong(partyModeControl.RandomlySelectedSong);
            }
            return;
        }

        CheckAudioAndShowPlayerSelectOverlay();
    }

    private void ShowPlayerSelectOverlay()
    {
        UpdatePlayerSelectOverlayButtons();
        songSelectPlayerSelectUi.ShowByDisplay();
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
        if (playerSelectStartSongButton.IsVisibleByDisplay())
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => playerSelectStartSongButton.Focus()));
        }
        else if (playerSelectCreateSongButton.IsVisibleByDisplay())
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => playerSelectCreateSongButton.Focus()));
        }
        else
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => closePlayerSelectOverlayButton.Focus()));
        }
    }

    public void HidePlayerSelectOverlay()
    {
        songSelectPlayerSelectUi.HideByDisplay();
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
        if (songSearchControl.GetSearchText().IsNullOrEmpty())
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
        IPlaylist playlist = SongSelectionPlaylistChooserControl.Selection.Value;
        List<SongMeta> filteredSongs = songSearchControl.GetFilteredSongMetas(songMetas)
            .Where(songMeta => playlist == null
                            || playlist.HasSongEntry(songMeta))
            .Where(songMeta => songSelectFilterControl.SongMetaPassesActiveFilters(songMeta))
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
                return SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
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
        bb.BindExistingInstance(SceneNavigator.GetSceneData(CreateDefaultSceneData()));
        bb.BindExistingInstance(songRouletteControl);
        bb.BindExistingInstance(songSelectSceneInputControl);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(SongOrderPickerControl);
        bb.BindExistingInstance(characterQuickJumpListControl);
        bb.BindExistingInstance(playerListControl);
        bb.BindExistingInstance(focusableNavigator);
        bb.BindExistingInstance(SongSelectionPlaylistChooserControl);
        bb.BindExistingInstance(createSingAlongSongControl);
        bb.BindExistingInstance(partyModeControl);
        bb.BindExistingInstance(songSelectFilterControl);
        bb.Bind(typeof(FocusableNavigator)).ToExistingInstance(focusableNavigator);
        bb.BindExistingInstance(songPreviewControl);
        return bb.GetBindings();
    }

    private SongSelectSceneData CreateDefaultSceneData()
    {
        return new SongSelectSceneData();
    }

    public void ToggleFavoritePlaylist()
    {
        SongSelectionPlaylistChooserControl.ToggleFavoritePlaylist();
    }

    public void ToggleSelectedSongIsFavorite()
    {
        if (SelectedSong == null)
        {
            return;
        }

        if (playlistManager.FavoritesPlaylist.HasSongEntry(SelectedSong))
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
        return SongSelectionPlaylistChooserControl.Selection.Value != null
               && !(SongSelectionPlaylistChooserControl.Selection.Value is UltraStarAllSongsPlaylist);
    }

    public void ResetPlaylistSelection()
    {
        SongSelectionPlaylistChooserControl.Reset();
    }

    private bool TryExecuteSpecialSearchSyntax(string searchText)
    {
        if (IsPartyModeRandomSongSelection)
        {
            return false;
        }

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
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_title);
        if (HasPartyModeSceneData)
        {
            sceneTitle.text += $" - {PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.roundsSettings.gameRoundSettings.Count}";
        }

        menuButton.text = TranslationManager.GetTranslation(R.Messages.menu);
        closeMenuOverlayButton.text = TranslationManager.GetTranslation(R.Messages.back);
        quitSongSelectButton.text = TranslationManager.GetTranslation(R.Messages.quit);
        toggleSongDetailOverlayButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_toggleSongDetailsButton);
        duetLegendLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_duetLegendLabel);
        videoLegendLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_videoLegendLabel);
        scoreModeLabel.text = TranslationManager.GetTranslation(R.Messages.options_scoreMode);
        noteDisplayModeLabel.text = TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode);
        noSongsFoundLabel.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_noSongsFound);
        downloadSongsButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_noSongsFound_downloadSongsButton);
        addSongFolderButton.text = TranslationManager.GetTranslation(R.Messages.songSelectScene_noSongsFound_addSongFolderButton);

        localHighScoreContainer.Q<Label>(R.UxmlNames.title).text = TranslationManager.GetTranslation(R.Messages.songSelectScene_localTopScoresTitle);
        onlineHighScoreContainer.Q<Label>(R.UxmlNames.title).text = TranslationManager.GetTranslation(R.Messages.songSelectScene_onlineTopScoresTitle);

        SongSelectionPlaylistChooserControl.UpdateTranslation();
        SongSearchControl.UpdateTranslation();
        songRouletteControl.UpdateTranslation();
        UpdateInputLegend();
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
               && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta);
    }

    public void SubmitSearch()
    {
        selectedSongBeforeSearch = SelectedSong;
        songSearchControl.ResetSearchText();
    }

    private void UpdateInputLegend()
    {
        inputLegend.Query<Label>()
            .Where(label => label is not FontIcon)
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

    public void ShowCannotUseJokerMessage()
    {
        UiManager.CreateNotification("No jokers left to change the song");
    }

    public List<PlayerProfile> GetEnabledPlayerProfiles()
    {
        if (!HasPartyModeSceneData)
        {
            return settings.PlayerProfiles
                .Where(playerProfile => playerProfile.IsEnabled)
                .ToList();
        }
        else if (PartyModeSettings.teamSettings.isFreeForAll)
        {
            // Select all players of all teams
            List<PlayerProfile> allPlayerProfiles = PartyModeUtils.GetAllPlayerProfiles(PartyModeSettings);
            return allPlayerProfiles
                .Where(playerProfile => playerProfile != null
                                        && !PartyModeUtils.IsKnockedOut(PartyModeSceneData, PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile)))
                .Distinct()
                .ToList();
        }
        else
        {
            // Select random player of each team
            List<PlayerProfile> result = new();
            PartyModeSettings.teamSettings.teams
                .Where(team => !PartyModeUtils.IsKnockedOut(PartyModeSceneData, team))
                .ForEach(team =>
                {
                    List<PlayerProfile> allTeamPlayerProfiles = team.playerProfiles.Union(team.guestPlayerProfiles).ToList();
                    PlayerProfile playerProfile = RandomUtils.RandomOf(allTeamPlayerProfiles);
                    result.Add(playerProfile);
                });
            return result
                .Where(playerProfile => playerProfile != null)
                .Distinct()
                .ToList();
        }
    }
}
