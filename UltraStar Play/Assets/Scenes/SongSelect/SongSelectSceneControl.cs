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
    public SongSelectSongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public SongSelectPlayerListControl playerListControl;

    [InjectedInInspector]
    public MicPitchTracker micPitchTrackerPrefab;
    
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

    [Inject(UxmlName = R.UxmlNames.songIndexLabel)]
    private Label songIndexLabel;

    [Inject(UxmlName = R.UxmlNames.songIndexContainer)]
    private VisualElement songIndexContainer;

    [Inject(UxmlName = R.UxmlNames.durationLabel)]
    private Label durationLabel;

    [Inject(UxmlName = R.UxmlNames.duetIcon)]
    private VisualElement duetIcon;

    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private MaterialIcon noFavoriteIcon;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private MaterialIcon favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.toggleFavoriteButton)]
    private Button toggleFavoriteButton;

    [Inject(UxmlName = R.UxmlNames.fuzzySearchTextLabel)]
    private Label fuzzySearchTextLabel;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startButton;

    [Inject(UxmlName = R.UxmlNames.quitSceneButton)]
    private Button quitSceneButton;

    [Inject(UxmlName = R.UxmlNames.localHighScoreContainer)]
    private VisualElement localHighScoreContainer;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongArtist)]
    private Label selectedSongArtist;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongTitle)]
    private Label selectedSongTitle;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongImageOuter)]
    private VisualElement selectedSongImageOuter;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongImageInner)]
    private VisualElement selectedSongImageInner;

    [Inject(UxmlName = R.UxmlNames.songOrderDropdownField)]
    private EnumField songOrderDropdownField;

    [Inject(UxmlName = R.UxmlNames.scoreModeLabel)]
    private Label scoreModeLabel;

    [Inject(UxmlName = R.UxmlNames.scoreModePicker)]
    private ItemPicker scoreModePicker;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModeLabel)]
    private Label noteDisplayModeLabel;

    [Inject(UxmlName = R.UxmlNames.noteDisplayModePicker)]
    private ItemPicker noteDisplayModePicker;

    [Inject(UxmlName = R.UxmlNames.toggleSingingOptionsButton)]
    private Button toggleSingingOptionsButton;

    [Inject(UxmlName = R.UxmlNames.showLyricsButton)]
    private Button showLyricsButton;
    
    [Inject(UxmlName = R.UxmlNames.playerList)]
    private VisualElement playerList;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsNewButton)]
    private Button addToSongQueueAsNewButton;
    
    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsMedleyButton)]
    private Button addToSongQueueAsMedleyButton;
    
    [Inject(UxmlName = R.UxmlNames.noScoresButton)]
    private ToggleButton noScoresButton;
    
    [Inject(UxmlName = R.UxmlNames.easyDifficultyButton)]
    private ToggleButton easyDifficultyButton;
    
    [Inject(UxmlName = R.UxmlNames.mediumDifficultyButton)]
    private ToggleButton mediumDifficultyButton;
    
    [Inject(UxmlName = R.UxmlNames.hardDifficultyButton)]
    private ToggleButton hardDifficultyButton;
    
    [Inject(UxmlName = R.UxmlNames.toggleCoopModeButton)]
    private Button toggleCoopModeButton;
    
    [Inject(UxmlName = R.UxmlNames.coopIcon)]
    private VisualElement coopIcon;
    
    [Inject(UxmlName = R.UxmlNames.noCoopIcon)]
    private VisualElement noCoopIcon;
    
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

    [Inject(UxmlName = R.UxmlNames.showSearchExpressionInfoButton)]
    private Button showSearchExpressionInfoButton;
    
    [Inject(UxmlName = R.UxmlNames.singingOptionsDropdownOverlay)]
    private VisualElement singingOptionsDropdownOverlay;
    
    [Inject(UxmlName = R.UxmlNames.singingOptionsDropdownContainer)]
    private VisualElement singingOptionsDropdownContainer;
    
    [Inject(UxmlName = R.UxmlNames.toggleMicCheckButton)]
    private ToggleButton toggleMicCheckButton;
    
    [Inject(UxmlName = R.UxmlNames.micCheckIcon)]
    private VisualElement micCheckIcon;
    
    [Inject(UxmlName = R.UxmlNames.noMicCheckIcon)]
    private VisualElement noMicCheckIcon;
    
    [Inject(UxmlName = R.UxmlNames.selectRandomSongButton)]
    private Button selectRandomSongButton;

    [Inject(UxmlName = R.UxmlNames.toggleSongQueueOverlayButton)]
    private Button toggleSongQueueOverlayButton;
    
    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;
    
    public SongSelectionPlaylistChooserControl SongSelectionPlaylistChooserControl { get; private set; }

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

    private MessageDialogControl searchExpressionHelpDialogControl;
    private MessageDialogControl lyricsDialogControl;

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

        InitSongMetas();

        if (HasPartyModeSceneData
            && PartyModeSettings.songSelectionSettings.songSelectionMode == EPartyModeSongSelectionMode.Random)
        {
            partyModeControl.SelectRandomSong();
        }
        
        InitModifiersChipsComboControl();
        
        InitDifficultyAndScoreMode();

        showLyricsButton.RegisterCallbackButtonTriggered(_ => ShowLyricsPopup());
        
        toggleMicCheckButton.RegisterCallbackButtonTriggered(_ => ToggleMicCheckActive());
        UpdateMicCheckButton();
        
        songOrderDropdownField.value = settings.SongSelectSettings.songOrder;
        songOrderDropdownField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"New order: {evt.newValue}");
            settings.SongSelectSettings.songOrder = (ESongOrder)evt.newValue;
            UpdateFilteredSongs();
        });

        // Register Callbacks
        toggleFavoriteButton.RegisterCallbackButtonTriggered(_ => ToggleSelectedSongIsFavorite());
        selectRandomSongButton.RegisterCallbackButtonTriggered(_ => SelectRandomSong());
        showSearchExpressionInfoButton.RegisterCallbackButtonTriggered(_ => ShowSearchExpressionHelpDialog());
        
        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.text = newValue);

        startButton.RegisterCallbackButtonTriggered(_ => AttemptStartSong());
        startButton.Focus();

        quitSceneButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.MainScene));

        songIndexContainer.RegisterCallback<PointerDownEvent>(evt => songSearchControl.SetSearchText($"#{SelectedSongIndex + 1}"));

        SongSearchControl.SearchChangedEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => OnSearchTextChanged());

        SongSelectionPlaylistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());
        songSelectFilterControl.FiltersChangedEventStream.Subscribe(_ => UpdateFilteredSongs());

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

        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationOfSongInMillis));

        downloadSongsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.ContentDownloadScene)));
        addSongFolderButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.SongLibraryOptionsScene)));

        // Show options in popup
        singingOptionsDropdownOverlay.HideByDisplay();
        toggleSingingOptionsButton.RegisterCallbackButtonTriggered(_ =>
        {
            singingOptionsDropdownOverlay.ToggleVisibleByDisplay();
        });
        VisualElementUtils.RegisterCallbackToHideByDisplayOnDirectClick(singingOptionsDropdownOverlay);
        new AnchoredPopupControl(singingOptionsDropdownContainer, toggleSingingOptionsButton, Corner2D.TopRight);

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
        });

        songQueueManager.SongQueueChangedEventStream
            .Subscribe(_ => songQueueUiControl.SetSongQueueEntryDtos(songQueueManager.GetSongQueueEntries()));

        toggleSongQueueOverlayButton.RegisterCallbackButtonTriggered(_ =>
        {
            songQueueOverlay.ToggleVisibleByDisplay();
        });
        addToSongQueueAsNewButton.RegisterCallbackButtonTriggered(_ => AddCurrentSongToSongQueue());
        addToSongQueueAsMedleyButton.RegisterCallbackButtonTriggered(_ => AddCurrentSongToSongQueueAsMedley());
        songQueueUiControl.OnToggleMedley = songQueueEntryDto => songQueueManager.ToggleMedley(songQueueEntryDto);
        songQueueUiControl.OnDelete = songQueueEntryDto => songQueueManager.RemoveSongQueueEntry(songQueueEntryDto);
    }

    private void UpdateMicCheckButton()
    {
        toggleMicCheckButton.SetActive(settings.SongSelectSettings.micTestActive);
        micCheckIcon.SetVisibleByDisplay(settings.SongSelectSettings.micTestActive);
        noMicCheckIcon.SetVisibleByDisplay(!settings.SongSelectSettings.micTestActive);
    }

    private void ToggleMicCheckActive()
    {
        settings.SongSelectSettings.micTestActive = !settings.SongSelectSettings.micTestActive;
        UpdateMicCheckButton();
        
        if (settings.SongSelectSettings.micTestActive)
        {
            FindObjectsOfType<MicSampleRecorder>()
                .Where(it => it.MicProfile != null && !it.IsRecording.Value)
                .ForEach(it => it.StartRecording());
        }
        else
        {
            FindObjectsOfType<MicSampleRecorder>()
                .Where(it => it.MicProfile != null && it.IsRecording.Value)
                .ForEach(it => it.StopRecording());
        }
    }

    private void InitDifficultyAndScoreMode()
    {
        // Set difficulty for all players
        settings.ObserveEveryValueChanged(it => it.GameSettings.Difficulty)
            .Subscribe(newValue => settings.PlayerProfiles.ForEach(it => it.Difficulty = newValue));

        GetDifficultyToButtonMap().ForEach(entry =>
        {
            entry.Value.RegisterCallbackButtonTriggered(_ =>
            {
                settings.GameSettings.Difficulty = entry.Key;
                if (settings.GameSettings.ScoreMode == EScoreMode.None)
                {
                    settings.GameSettings.ScoreMode = EScoreMode.Individual;
                }
                UpdateDifficultyAndScoreModeButtons();
                UpdateSongStatistics(SelectedSong);
            });
        });
        UpdateDifficultyAndScoreModeButtons();
        
        noScoresButton.RegisterCallbackButtonTriggered(_ =>
        {
            settings.GameSettings.ScoreMode = EScoreMode.None;
            UpdateDifficultyAndScoreModeButtons();
        });
        
        toggleCoopModeButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (settings.GameSettings.ScoreMode == EScoreMode.CommonAverage)
            {
                settings.GameSettings.ScoreMode = EScoreMode.Individual;
            }
            else
            {
                settings.GameSettings.ScoreMode = EScoreMode.CommonAverage;
            }
            UpdateDifficultyAndScoreModeButtons();
        });
    }

    private void UpdateDifficultyAndScoreModeButtons()
    {
        GetDifficultyToButtonMap().ForEach(entry =>
        {
            bool isSelectedDifficulty = settings.GameSettings.Difficulty == entry.Key;
            entry.Value.SetActive(isSelectedDifficulty && settings.GameSettings.ScoreMode != EScoreMode.None);
        });
        noScoresButton.SetActive(settings.GameSettings.ScoreMode == EScoreMode.None);
        coopIcon.SetVisibleByDisplay(settings.GameSettings.ScoreMode == EScoreMode.CommonAverage);
        noCoopIcon.SetVisibleByDisplay(settings.GameSettings.ScoreMode != EScoreMode.CommonAverage);
    }

    private void InitModifiersChipsComboControl()
    {
        injector.Inject(gameRoundSettingsUiControl);
        gameRoundSettingsUiControl.GameRoundSettings = settings.GameRoundSettings;
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

    private void ShowLyricsPopup()
    {
        if (lyricsDialogControl != null
            || SelectedSong == null)
        {
            return;
        }

        lyricsDialogControl = uiManager.CreateDialogControl($"{SelectedSong.Title}");
        lyricsDialogControl.DialogClosedEventStream.Subscribe(_ => lyricsDialogControl = null);
        
        Label CreateLyricsLabel(string lyrics)
        {
            Label lyricsLabel = new Label(lyrics);
            lyricsLabel.enableRichText = true;
            lyricsLabel.AddToClassList("songSelectLyricsPreview");
            return lyricsLabel;
        }
        
        if (SelectedSong.GetVoices().Count < 2)
        {
            string lyrics = SongMetaUtils.GetLyrics(SelectedSong, Voice.firstVoiceName);
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(lyrics));
        }
        else
        {
            string firstVoiceLyrics = $"<i><b>{SelectedSong.VoiceNames.FirstOrDefault().Value}</b></i>\n\n" 
                                      + SongMetaUtils.GetLyrics(SelectedSong, Voice.firstVoiceName);
            string secondVoiceLyrics = $"<i><b>{SelectedSong.VoiceNames.LastOrDefault().Value}</b></i>\n\n" 
                                       + SongMetaUtils.GetLyrics(SelectedSong, Voice.secondVoiceName);
            
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(firstVoiceLyrics));
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(secondVoiceLyrics));
        }
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(lyricsDialogControl.DialogRootVisualElement);
    }
    
    private void ShowSearchExpressionHelpDialog()
    {
        if (searchExpressionHelpDialogControl != null)
        {
            return;
        }
        
        Dictionary<string, string> titleToContentMap = new()
        {
            { "Search Expressions",
                TranslationManager.GetTranslation(R.Messages.songSelectScene_searchExpressionInfo) },
            { "Syntax",
                TranslationManager.GetTranslation(R.Messages.songSelectScene_searchExpressionInfo_syntaxTips) },
        };
        searchExpressionHelpDialogControl = uiManager.CreateHelpDialogControl(
            "Advanced Search Expressions",
            titleToContentMap);
        searchExpressionHelpDialogControl.DialogClosedEventStream.Subscribe(_ => searchExpressionHelpDialogControl = null);
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(searchExpressionHelpDialogControl.DialogRootVisualElement);
    }
    
    public void CloseSearchExpressionHelp()
    {
        searchExpressionHelpDialogControl?.CloseDialog();
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
        songRouletteControl.Selection.Subscribe(newValue => OnSongSelectionChanged(newValue));

        if (sceneData.SongMeta != null)
        {
            songRouletteControl.SelectSong(sceneData.SongMeta);
        }
    }

    private void OnSongSelectionChanged(SongSelection selection)
    {
        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            songIndexLabel.text = "-";
            return;
        }

        selectedSongArtist.text = selectedSong.Artist;
        selectedSongTitle.text = selectedSong.Title;
        SongMetaImageUtils.SetCoverOrBackgroundImage(selection.SongMeta, selectedSongImageInner, selectedSongImageOuter);
        songIndexLabel.text = $"{selection.SongIndex + 1} / {selection.SongsCount}";

        // The song duration requires loading the audio file.
        // Loading every song only to show its duration is slow (e.g. when scrolling through songs).
        // Instead, the label is updated when the AudioClip has been loaded.
        durationLabel.text = "";

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIcon.SetVisibleByVisibility(isDuet);

        UpdateFavoriteIcon();

        UpdateSongStatistics(selectedSong);

        UpdateModifiersChipsCombo();

        // Choose lyrics for duet song
        playerListControl.UpdateVoiceSelection();
    }

    private void UpdateModifiersChipsCombo()
    {
        
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
            List<SongStatistic> topScores = localStatistic.StatsEntries.GetTopScores(1, settings.GameSettings.Difficulty);
            List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();

            UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);
        }
        else
        {
            UpdateTopScoreLabels(new List<int>(), localHighScoreContainer);
        }
    }

    private void UpdateTopScoreLabels(List<int> topScores, VisualElement labelContainer)
    {
        List<Label> labels = labelContainer.Query<Label>().ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            string scoreText = topScores.Count >= i + 1
                ? topScores[i].ToString()
                : "-";
            
            labels[i].text = scoreText;
        }
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
        singSceneData.gameRoundSettings = new(settings.GameRoundSettings);

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
        if (!songQueueManager.IsSongQueueEmpty)
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
        SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(sceneData.partyModeSceneData);
        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
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
        selectedSongArtist.text = "";
        selectedSongTitle.text = "";
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
        if (!songAudioPlayer.IsPartiallyLoaded)
        {
            string message = $"Audio file '{SelectedSong.Mp3}' could not be loaded.\nPlease use a supported format.";
            Debug.Log(message);
            UiManager.CreateNotification(message);
            return;
        }

        // Start the sing scene or show the player select overlay.
        StartSingScene();
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
        switch (songOrderDropdownField.value)
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
                Debug.LogWarning("Unknown order for songs: " + songOrderDropdownField.value);
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
        bb.BindExistingInstance(playerListControl);
        bb.BindExistingInstance(SongSelectionPlaylistChooserControl);
        bb.BindExistingInstance(createSingAlongSongControl);
        bb.BindExistingInstance(partyModeControl);
        bb.BindExistingInstance(songSelectFilterControl);
        bb.BindExistingInstance(songPreviewControl);
        bb.Bind(nameof(micPitchTrackerPrefab)).ToExistingInstance(micPitchTrackerPrefab);
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
        List<SongMeta> filteredSongMetas = GetFilteredSongMetas();
        if (!filteredSongMetas.IsNullOrEmpty()
            && filteredSongMetas.SequenceEqual(songRouletteControl.Songs))
        {
            return;
        }
        songRouletteControl.SetSongs(filteredSongMetas);
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
            sceneTitle.text += $" - {PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.roundCount}";
        }

        startButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_label);
        scoreModeLabel.text = TranslationManager.GetTranslation(R.Messages.options_scoreMode);
        noteDisplayModeLabel.text = TranslationManager.GetTranslation(R.Messages.options_noteDisplayMode);

        SongSearchControl.UpdateTranslation();
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
        startButton.Focus();
    }

    private void UpdateInputLegend()
    {
        // inputLegend.Query<Label>()
        //     .Where(label => label is not FontIcon)
        //     .ForEach(label => label.RemoveFromHierarchy());
        //
        // if (IsPlayerSelectOverlayVisible)
        // {
        //     InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
        //         TranslationManager.GetTranslation(R.Messages.back),
        //         inputLegend);
        //     InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_togglePlayers,
        //         TranslationManager.GetTranslation(R.Messages.action_togglePlayers),
        //         inputLegend);
        // }
        // else
        // {
        //     InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
        //         TranslationManager.GetTranslation(R.Messages.back),
        //         inputLegend);
        //     InputLegendControl.TryAddInputActionInfo(R.InputActions.ui_submit,
        //         TranslationManager.GetTranslation(R.Messages.submit),
        //         inputLegend);
        //     InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_toggleSongMenu,
        //         TranslationManager.GetTranslation(R.Messages.action_openSongMenu),
        //         inputLegend);
        // }
        // if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        // {
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         TranslationManager.GetTranslation(R.Messages.action_openSongMenu),
        //         TranslationManager.GetTranslation(R.Messages.action_longPress))));
        // }
    }

    private Dictionary<EDifficulty, ToggleButton> GetDifficultyToButtonMap()
    {
        return new Dictionary<EDifficulty, ToggleButton>
        {
            { EDifficulty.Easy, easyDifficultyButton },
            { EDifficulty.Medium, mediumDifficultyButton },
            { EDifficulty.Hard, hardDifficultyButton },
        };
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
