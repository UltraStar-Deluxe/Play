using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneControl : MonoBehaviour, INeedInjection, IBinder, IInjectionFinishedListener
{
    private static readonly ProfilerMarker onInjectionFinishedProfilerMarker = new ProfilerMarker("SongSelectSceneControl.OnInjectionFinished");
    private readonly IComparer<object> songMetaPropertyComparer = new NullOrEmptyValueLastComparer();

    private const string VirtualRootFolderName = "SONG_SELECT_ROOT";

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
    public NewestSamplesMicPitchTracker micPitchTrackerPrefab;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.fuzzySearchTextLabel)]
    private Label fuzzySearchTextLabel;

    [Inject(UxmlName = R.UxmlNames.quitSceneButton)]
    private Button quitSceneButton;

    [Inject(UxmlName = R.UxmlNames.songOrderDropdownField)]
    private EnumField songOrderDropdownField;

    [Inject(UxmlName = R.UxmlNames.playerList)]
    private VisualElement playerList;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsNewButton)]
    private Button addToSongQueueAsNewButton;

    [Inject(UxmlName = R.UxmlNames.addToSongQueueAsMedleyButton)]
    private Button addToSongQueueAsMedleyButton;

    [Inject(UxmlName = R.UxmlNames.startSongQueueButton)]
    private Button startSongQueueButton;

    [Inject(UxmlName = R.UxmlNames.toggleCoopModeButton)]
    private Button toggleCoopModeButton;

    [Inject(UxmlName = R.UxmlNames.coopIcon)]
    private VisualElement coopIcon;

    [Inject(UxmlName = R.UxmlNames.noCoopIcon)]
    private VisualElement noCoopIcon;

    [Inject(UxmlName = R.UxmlNames.navigateFolderUpButton)]
    private Button navigateFolderUpButton;

    [Inject]
    private AchievementEventStream achievementEventStream;

    [Inject]
    private SongSelectSceneData sceneData;

    private List<SongMeta> songMetas = new();
    private List<SongMeta> lastSongMetasOfSongRouletteControl = new();
    private DirectoryInfo lastDirectoryInfoOfSongRouletteControl;
    private float lastSongMetaCountUpdateTimeInSeconds;
    private string lastRawSearchText;
    private SongSelectEntry selectedEntryBeforeSearch;

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
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongIssueManager songIssueManager;

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject(UxmlName = R.UxmlNames.noSongsFoundContainer)]
    private VisualElement noSongsFoundContainer;

    [Inject(UxmlName = R.UxmlNames.songScanInProgressContainer)]
    private VisualElement songScanInProgressContainer;

    [Inject(UxmlName = R.UxmlNames.songScanInProgressProgressLabel)]
    private Label songScanInProgressProgressLabel;

    [Inject(UxmlName = R.UxmlNames.importSongsButton)]
    private Button importSongsButton;

    [Inject(UxmlName = R.UxmlNames.showSearchExpressionInfoButton)]
    private Button showSearchExpressionInfoButton;

    [Inject(UxmlName = R.UxmlNames.searchExpressionToggle)]
    private Toggle searchExpressionToggle;

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

    [Inject(UxmlName = R.UxmlNames.closeSongQueueButton)]
    private Button closeSongQueueButton;

    [Inject(UxmlName = R.UxmlNames.songQueueLengthContainer)]
    private VisualElement songQueueLengthContainer;

    [Inject(UxmlName = R.UxmlNames.songQueueLengthLabel)]
    private Label songQueueLengthLabel;

    [Inject(UxmlName = R.UxmlNames.songQueueOverlay)]
    private VisualElement songQueueOverlay;

    [Inject(UxmlName = R.UxmlNames.toggleModifiersOverlayButton)]
    private Button toggleModifiersOverlayButton;

    [Inject(UxmlName = R.UxmlNames.modifiersActiveIcon)]
    private VisualElement modifiersActiveIcon;

    [Inject(UxmlName = R.UxmlNames.hiddenHideSongQueueOverlayArea)]
    private VisualElement hiddenHideSongQueueOverlayArea;

    [Inject(UxmlName = R.UxmlNames.hiddenHideModifiersOverlayArea)]
    private VisualElement hiddenHideModifiersOverlayArea;

    [Inject(UxmlName = R.UxmlNames.modifiersInactiveIcon)]
    private VisualElement modifiersInactiveIcon;

    [Inject(UxmlName = R.UxmlNames.closeModifiersOverlayButton)]
    private Button closeModifiersOverlayButton;

    [Inject(UxmlName = R.UxmlNames.modifierDialogOverlay)]
    private VisualElement modifierDialogOverlay;

    [Inject(UxmlName = R.UxmlNames.currentDifficultyLabel)]
    private Label currentDifficultyLabel;

    [Inject(UxmlName = R.UxmlNames.nextDifficultyButton)]
    private Button nextDifficultyButton;

    [Inject(UxmlName = R.UxmlNames.previousDifficultyButton)]
    private Button previousDifficultyButton;

    private readonly SongSearchControl songSearchControl = new();

    public ReactiveProperty<bool> IsSongRepositorySearchRunning { get; private set; } = new(false);

    public SongMeta SelectedSong
    {
        get
        {
            return (songRouletteControl.SelectedEntry as SongSelectSongEntry)?.SongMeta;
        }
    }

    private MessageDialogControl searchExpressionHelpDialogControl;
    private MessageDialogControl lyricsDialogControl;
    private MessageDialogControl noSingAlongDataDialogControl;

    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSettings PartyModeSettings => PartyModeSceneData.PartyModeSettings;
    public bool IsPartyModeRandomSongSelection => HasPartyModeSceneData
                                                  && PartyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random;
    public bool UsePartyModePlaylist => IsPartyModeRandomSongSelection
                                        && PartyModeSettings.SongSelectionSettings.SongPoolPlaylist != null;
    public bool CanUseSongSelectionJoker => PartyModeSceneData.remainingJokerCount != 0;

    public SongSelectionPlaylistChooserControl SongSelectionPlaylistChooserControl { get; private set; } = new();
    private readonly CreateSingAlongSongControl createSingAlongSongControl = new();
    private readonly SongSelectScenePartyModeControl partyModeControl = new();
    private readonly GameRoundModifierDialogControl modifierDialogControl = new();
    private readonly SongQueueUiControl songQueueUiControl = new();
    private readonly SongSelectFilterControl songSelectFilterControl = new();
    private readonly SongSelectSelectedSongDetailsControl songSelectSelectedSongDetailsControl = new();

    private MessageDialogControl askToAssignMicsDialog;

    public VisualElementSlideInControl SongQueueSlideInControl { get; private set; }
    public VisualElementSlideInControl ModifiersOverlaySlideInControl { get; private set; }

    private readonly Subject<BeforeSongStartedEvent> beforeSongStartedEventStream = new();
    public IObservable<BeforeSongStartedEvent> BeforeSongStartedEventStream => beforeSongStartedEventStream;

    public void OnInjectionFinished()
    {
        using IDisposable d = onInjectionFinishedProfilerMarker.Auto();

        injector.Inject(SongSelectionPlaylistChooserControl);
        injector.Inject(createSingAlongSongControl);
        injector.Inject(partyModeControl);
        injector.Inject(songQueueUiControl);
        injector.Inject(songSelectFilterControl);
        injector.Inject(songSearchControl);
        injector.Inject(songSelectSelectedSongDetailsControl);
    }

    private void Start()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.Start");

        songMetaManager.ScanSongsIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!songMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        songMetaManager.SongScanFinishedEventStream
            .Subscribe(evt =>
            {
                UpdateAvailableSongsAndUi(songMetaManager.IsSongScanFinished || evt != null);
            })
            .AddTo(gameObject);
        songMetaManager.AddedSongMetaEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 1000))
            .Subscribe(_ => UpdateAvailableSongsAndUi(songMetaManager.IsSongScanFinished));
        UpdateSongScanLabels(songMetaManager.IsSongScanFinished);

        InitSongMetas();

        if (HasPartyModeSceneData
            && PartyModeSettings.SongSelectionSettings.SongSelectionMode == EPartyModeSongSelectionMode.Random)
        {
            partyModeControl.SelectRandomSong();
        }

        InitDifficultyAndScoreMode();

        toggleMicCheckButton.RegisterCallbackButtonTriggered(_ => nonPersistentSettings.MicTestActive.Value = !nonPersistentSettings.MicTestActive.Value);
        nonPersistentSettings.MicTestActive.Subscribe(_ => UpdateMicCheckButton());
        UpdateMicCheckButton();

        songOrderDropdownField.value = settings.SongOrder;
        songOrderDropdownField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log($"New order: {evt.newValue}");
            settings.SongOrder = (ESongOrder)evt.newValue;
            UpdateFilteredSongs();
        });

        selectRandomSongButton.RegisterCallbackButtonTriggered(_ => SelectRandomSong());
        FieldBindingUtils.Bind(searchExpressionToggle,
            () => nonPersistentSettings.IsSearchExpressionsEnabled.Value,
            newValue => nonPersistentSettings.IsSearchExpressionsEnabled.Value = newValue);
        showSearchExpressionInfoButton.RegisterCallbackButtonTriggered(_ => ShowSearchExpressionHelp());

        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.SetTranslatedText(Translation.Of(newValue)));

        songRouletteControl.SubmitEventStream.Subscribe(_ => OnSubmitSongRoulette());
        songRouletteControl.Focus();

        quitSceneButton.RegisterCallbackButtonTriggered(_ => QuitSongSelect());

        songSearchControl.SearchChangedEventStream
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => OnSearchTextChanged());
        songSearchControl.SubmitEventStream.Subscribe(_ => OnSubmitSearch());

        SongSelectionPlaylistChooserControl.Selection.Subscribe(_ => UpdateFilteredSongs());
        songSelectFilterControl.FiltersChangedEventStream.Subscribe(_ => UpdateFilteredSongs());

        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(it =>
            {
                if (songOrderDropdownField.value is ESongOrder.LocalHighScore)
                {
                    UpdateFilteredSongs();
                }
            });

        playlistManager.PlaylistChangeEventStream.Subscribe(playlistChangeEvent =>
        {
            if (playlistChangeEvent.Playlist == SongSelectionPlaylistChooserControl.Selection.Value)
            {
                UpdateFilteredSongs();
            }
        });

        InitSongRoulette();

        importSongsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.SongLibraryOptionsScene)));

        if (settings.NavigateByFoldersInSongSelect)
        {
            navigateFolderUpButton.RegisterCallbackButtonTriggered(_ => TryNavigateToParentFolder());
        }
        navigateFolderUpButton.SetVisibleByDisplay(settings.NavigateByFoldersInSongSelect);

        createSingAlongSongControl.CreatedSingAlongVersionEventStream.Subscribe(processedSongMeta =>
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songEditor_createdSingAlongDataSuccess,
                "name", Path.GetFileName(processedSongMeta.Audio)));
        });

        // Song queue
        InitSongQueue();

        // Init modifier dialog
        InitModifierDialog();

        // Hide slide-in controls with click outside
        InitHideSlideInControlsViaClick();

        UpdateTranslation();
    }

    private void UpdateAvailableSongsAndUi(bool isSongScanFinished)
    {
        InitSongMetas();
        UpdateFilteredSongs();
        UpdateSongScanLabels(isSongScanFinished);
    }

    private void InitSongQueue()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongQueueOverlay");

        songQueueLengthContainer.HideByDisplay();
        songQueueManager.SongQueueChangedEventStream
            .Subscribe(_ => UpdateSongQueue())
            .AddTo(gameObject);
        UpdateSongQueue();

        songQueueOverlay.ShowByDisplay();
        SongQueueSlideInControl = new(songQueueOverlay, ESide2D.Right, false);
        toggleSongQueueOverlayButton.RegisterCallbackButtonTriggered(_ => SongQueueSlideInControl.ToggleVisible());
        closeSongQueueButton.RegisterCallbackButtonTriggered(_ => SongQueueSlideInControl.SlideOut());
        addToSongQueueAsNewButton.RegisterCallbackButtonTriggered(_ => AddSongToSongQueue(SelectedSong));
        addToSongQueueAsMedleyButton.RegisterCallbackButtonTriggered(_ => AddSongToSongQueueAsMedley(SelectedSong));
        startSongQueueButton.RegisterCallbackButtonTriggered(_ => StartSingSceneWithNextSongQueueEntry());
        songQueueUiControl.OnToggleMedley = songQueueEntryDto => songQueueManager.ToggleMedley(songQueueEntryDto);
        songQueueUiControl.OnDelete = songQueueEntryDto => songQueueManager.RemoveSongQueueEntry(songQueueEntryDto);
    }

    private void InitHideSlideInControlsViaClick()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitHideSlideInControlsViaClick");

        hiddenHideModifiersOverlayArea.HideByDisplay();
        hiddenHideModifiersOverlayArea.RegisterCallback<PointerDownEvent>(_ => ModifiersOverlaySlideInControl.SlideOut());
        ModifiersOverlaySlideInControl.Visible.Subscribe(newValue =>
        {
            hiddenHideModifiersOverlayArea.SetVisibleByDisplay(newValue);
            if (newValue)
            {
                closeModifiersOverlayButton.Focus();
            }
            else if (VisualElementUtils.IsDescendantFocused(modifierDialogOverlay))
            {
                toggleModifiersOverlayButton.Focus();
            }
        });

        hiddenHideSongQueueOverlayArea.HideByDisplay();
        hiddenHideSongQueueOverlayArea.RegisterCallback<PointerDownEvent>(_ => SongQueueSlideInControl.SlideOut());
        SongQueueSlideInControl.Visible.Subscribe(newValue =>
        {
            hiddenHideSongQueueOverlayArea.SetVisibleByDisplay(newValue);
            if (newValue)
            {
                closeSongQueueButton.Focus();
            }
            else if (VisualElementUtils.IsDescendantFocused(songQueueOverlay))
            {
                toggleSongQueueOverlayButton.Focus();
            }
        });
    }

    private void InitModifierDialog()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitModifierDialog");

        // Modifier dialog overlay
        modifierDialogOverlay.ShowByDisplay();
        ModifiersOverlaySlideInControl = new(modifierDialogOverlay, ESide2D.Right, false);
        toggleModifiersOverlayButton.RegisterCallbackButtonTriggered(_ => ModifiersOverlaySlideInControl.ToggleVisible());
        closeModifiersOverlayButton.RegisterCallbackButtonTriggered(_ => ModifiersOverlaySlideInControl.SlideOut());

        // Modifier active icon
        modifiersActiveIcon.HideByDisplay();
        nonPersistentSettings.ObserveEveryValueChanged(it => it.GameRoundSettings.AnyModifierActive)
            .Subscribe(_ => UpdateModifiersActiveIcon());

        // Delay initialization of modifier dialog control
        bool initializedModifierDialogControl = false;
        ModifiersOverlaySlideInControl.Visible.Subscribe(newValue =>
        {
            if (newValue
                && !initializedModifierDialogControl)
            {
                initializedModifierDialogControl = true;
                InitModifierDialogControl();
            }
        });
    }

    private void InitModifierDialogControl()
    {
        injector.WithRootVisualElement(modifierDialogOverlay)
            .Inject(modifierDialogControl);
        modifierDialogControl.OpenDialog(nonPersistentSettings.GameRoundSettings);
        modifierDialogOverlay.Query(R_PlayShared.UxmlNames.closeModifierDialogButton).ForEach(it => it.HideByDisplay());
    }

    private void UpdateSongQueue()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.UpdateSongQueue");

        string newSongQueueLengthAsString = songQueueManager.SongQueueLength.ToString();
        if (songQueueLengthLabel.text != newSongQueueLengthAsString)
        {
            songQueueLengthContainer.SetVisibleByDisplay(songQueueManager.SongQueueLength > 0);
            songQueueLengthLabel.SetTranslatedText(Translation.Of(newSongQueueLengthAsString));
            LeanTween.value(gameObject, Vector3.one * 1.5f, Vector3.one, 1.5f)
                .setEaseOutBounce()
                .setOnUpdate(s => songQueueLengthLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s))));
        }
        songQueueUiControl.SetSongQueueEntryDtos(songQueueManager.GetSongQueueEntries());
        startSongQueueButton.SetEnabled(!songQueueManager.IsSongQueueEmpty);
    }

    private void UpdateModifiersActiveIcon()
    {
        modifiersActiveIcon.SetVisibleByDisplay(nonPersistentSettings.GameRoundSettings.AnyModifierActive);
        modifiersInactiveIcon.SetVisibleByDisplay(!nonPersistentSettings.GameRoundSettings.AnyModifierActive);
    }

    private void UpdateMicCheckButton()
    {
        toggleMicCheckButton.SetActive(nonPersistentSettings.MicTestActive.Value);
        micCheckIcon.SetVisibleByDisplay(nonPersistentSettings.MicTestActive.Value);
        noMicCheckIcon.SetVisibleByDisplay(!nonPersistentSettings.MicTestActive.Value);
    }

    private void InitDifficultyAndScoreMode()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitDifficultyAndScoreMode");

        // Set difficulty for all players
        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(newValue =>
            {
                settings.PlayerProfiles
                    .Union(nonPersistentSettings.LobbyMemberPlayerProfiles)
                    .ForEach(it => it.Difficulty = newValue);
            });

        nextDifficultyButton.RegisterCallbackButtonTriggered(_ => SetNextDifficulty());
        previousDifficultyButton.RegisterCallbackButtonTriggered(_ => SetPreviousDifficulty());

        UpdateDifficultyAndScoreModeControls();

        toggleCoopModeButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (settings.ScoreMode == EScoreMode.CommonAverage)
            {
                settings.ScoreMode = EScoreMode.Individual;
            }
            else
            {
                settings.ScoreMode = EScoreMode.CommonAverage;
            }
            UpdateDifficultyAndScoreModeControls();
        });
    }

    private void SetPreviousDifficulty()
    {
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
            SetDifficulty(EDifficulty.Hard);
        }
        else
        {
            switch (settings.Difficulty)
            {
                case EDifficulty.Easy:
                    SetNoScoreMode();
                    break;
                case EDifficulty.Medium:
                    SetDifficulty(EDifficulty.Easy);
                    break;
                case EDifficulty.Hard:
                    SetDifficulty(EDifficulty.Medium);
                    break;
            }
        }
    }

    private void SetNextDifficulty()
    {
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
            SetDifficulty(EDifficulty.Easy);
        }
        else
        {
            switch (settings.Difficulty)
            {
                case EDifficulty.Easy:
                    SetDifficulty(EDifficulty.Medium);
                    break;
                case EDifficulty.Medium:
                    SetDifficulty(EDifficulty.Hard);
                    break;
                case EDifficulty.Hard:
                    SetNoScoreMode();
                    break;
            }
        }
    }

    private void SetNoScoreMode()
    {
        settings.ScoreMode = EScoreMode.None;
        UpdateDifficultyAndScoreModeControls();
    }

    private void SetDifficulty(EDifficulty difficulty)
    {
        settings.Difficulty = difficulty;
        if (settings.ScoreMode == EScoreMode.None)
        {
            settings.ScoreMode = EScoreMode.Individual;
        }
        UpdateDifficultyAndScoreModeControls();
    }

    private void UpdateDifficultyAndScoreModeControls()
    {
        coopIcon.SetVisibleByDisplay(settings.ScoreMode == EScoreMode.CommonAverage);
        noCoopIcon.SetVisibleByDisplay(settings.ScoreMode != EScoreMode.CommonAverage);

        if (settings.ScoreMode == EScoreMode.None)
        {
            currentDifficultyLabel.SetTranslatedText(Translation.Get(R.Messages.options_difficulty_noScores));
        }
        else
        {
            currentDifficultyLabel.SetTranslatedText(Translation.Get(settings.Difficulty));
        }
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

    public void AddSongToSongQueue(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings(songMeta);
        songQueueManager.AddSongQueueEntry(songQueueEntryDto);
    }

    public void AddSongToSongQueueAsMedley(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        if (songQueueManager.IsSongQueueEmpty)
        {
            // Cannot create medley with previous song when song queue is empty.
            AddSongToSongQueue(songMeta);
            return;
        }

        SongQueueEntryDto songQueueEntryDto = CreateSongQueueEntryWithCurrentSettings(songMeta);
        if (songQueueEntryDto != null)
        {
            songQueueEntryDto.IsMedleyWithPreviousEntry = true;
            songQueueManager.AddSongQueueEntry(songQueueEntryDto);
        }
    }

    private SongQueueEntryDto CreateSongQueueEntryWithCurrentSettings(SongMeta songMeta)
    {
        SongQueueEntryDto songQueueEntryDto = new();
        songQueueEntryDto.SongDto = DtoConverter.ToDto(songMeta);
        songQueueEntryDto.SingScenePlayerDataDto = DtoConverter.ToDto(CreateSingScenePlayerData());
        songQueueEntryDto.GameRoundSettingsDto = new GameRoundSettingsDto()
        {
            ModifierDtos = DtoConverter.ToDto(nonPersistentSettings.GameRoundSettings.modifiers),
        };
        return songQueueEntryDto;
    }

    private void ShowSearchExpressionHelp()
    {
        ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_howToSearchExpressions));
    }

    public void ShowLyricsAndInfoPopup(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        if (lyricsDialogControl != null)
        {
            lyricsDialogControl.CloseDialog();
        }

        lyricsDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_lyricsDialog_title,
            "songName", songMeta.Title));
        lyricsDialogControl.DialogClosedEventStream.Subscribe(_ => lyricsDialogControl = null);

        Label CreateLyricsLabel(string lyrics)
        {
            Label lyricsLabel = new Label(lyrics);
            lyricsLabel.enableRichText = true;
            lyricsLabel.AddToClassList("songSelectLyricsPreview");
            return lyricsLabel;
        }

        if (songMeta.VoiceCount < 2)
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(lyrics));
        }
        else
        {
            string firstVoiceLyrics = $"<i><b>{songMeta.GetVoiceDisplayName(EVoiceId.P1)}</b></i>\n\n"
                                      + SongMetaUtils.GetLyrics(songMeta, EVoiceId.P1);
            string secondVoiceLyrics = $"<i><b>{songMeta.GetVoiceDisplayName(EVoiceId.P2)}</b></i>\n\n"
                                       + SongMetaUtils.GetLyrics(songMeta, EVoiceId.P2);

            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(firstVoiceLyrics));
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(secondVoiceLyrics));
        }

        // Add attribution and license info
        AccordionItem attributionAccordionItem = new(Translation.Get(R.Messages.action_showAttribution));
        attributionAccordionItem.Add(AttributionUtils.CreateAttributionVisualElement(songMeta));
        lyricsDialogControl.AddVisualElement(attributionAccordionItem);
    }

    public void InitSongMetas()
    {
        if (!songMetaManager.IsSongScanFinished)
        {
            return;
        }

        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongMetas");

        songMetas = new List<SongMeta>(songMetaManager.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));

        // Trigger achievement
        if (songMetas.Count > 100)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.browseMoreThan100Songs));
        }
    }

    private void UpdateSongScanLabels(bool isSongScanFinished)
    {
        songScanInProgressProgressLabel.SetTranslatedText(Translation.Of($"{songMetaManager.LoadedSongsPercent:00} %"));

        if (isSongScanFinished)
        {
            songScanInProgressContainer.HideByDisplay();
            noSongsFoundContainer.SetVisibleByDisplay(songMetaManager.LoadedSongsCount <= 0);
        }
        else
        {
            songScanInProgressContainer.ShowByDisplay();
            noSongsFoundContainer.HideByDisplay();
        }
    }

    private void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (!songMetaManager.IsSongScanFinished
            && Time.time - lastSongMetaCountUpdateTimeInSeconds > 1f )
        {
            lastSongMetaCountUpdateTimeInSeconds = Time.time;
            UpdateSongScanLabels(songMetaManager.IsSongScanFinished);
        }
    }

    private void InitSongRoulette()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongRouletteSongMetas");

        UpdateFilteredSongs();
        songRouletteControl.Selection.Subscribe(newValue => songSelectSelectedSongDetailsControl.OnSongSelectionChanged(newValue));
        songRouletteControl.SelectionClickedEventStream
            .Subscribe(_ => AttemptStartSelectedEntry());

        if (sceneData.SongMeta != null)
        {
            songRouletteControl.SelectEntryBySongMeta(sceneData.SongMeta);
        }
    }

    public void DoFuzzySearch(string text)
    {
        string searchTextNoWhitespace = text.Replace(" ", "");
        if (searchTextNoWhitespace.IsNullOrEmpty())
        {
            return;
        }

        // Try to jump to song-index
        if (TryExecuteSpecialSearchSyntax(text))
        {
            return;
        }

        string GetEntryTitle(SongSelectEntry songSelectEntry)
        {
            if (songSelectEntry is SongSelectSongEntry songEntry)
            {
                return songEntry.SongMeta.Title;
            }
            else if (songSelectEntry is SongSelectFolderEntry folderEntry)
            {
                return folderEntry.DirectoryInfo.Name;
            }
            else
            {
                return "";
            }
        }

        string GetEntryArtist(SongSelectEntry songSelectEntry)
        {
            if (songSelectEntry is SongSelectSongEntry songEntry)
            {
                return songEntry.SongMeta.Artist;
            }
            else
            {
                return "";
            }
        }

        // Search title that starts with the text
        SongSelectEntry titleStartsWithMatch = songRouletteControl.Find(it =>
        {
            string titleNoWhitespace = GetEntryTitle(it).Replace(" ", "");
            return StringUtils.StartsWithIgnoreCaseAndDiacritics(titleNoWhitespace, searchTextNoWhitespace);
        });
        if (titleStartsWithMatch != null)
        {
            songRouletteControl.SelectEntry(titleStartsWithMatch);
            return;
        }

        // Search artist that starts with the text
        SongSelectEntry artistStartsWithMatch = songRouletteControl.Find(it =>
        {
            string artistNoWhitespace = GetEntryArtist(it).Replace(" ", "");
            return StringUtils.StartsWithIgnoreCaseAndDiacritics(artistNoWhitespace, searchTextNoWhitespace);
        });
        if (artistStartsWithMatch != null)
        {
            songRouletteControl.SelectEntry(artistStartsWithMatch);
            return;
        }

        // Search title or artist contains the text
        SongSelectEntry artistOrTitleContainsMatch = songRouletteControl.Find(it =>
        {
            string artistNoWhitespace = GetEntryArtist(it).Replace(" ", "");
            string titleNoWhitespace = GetEntryTitle(it).Replace(" ", "");
            return StringUtils.ContainsIgnoreCaseAndDiacritics(artistNoWhitespace, searchTextNoWhitespace)
                || StringUtils.ContainsIgnoreCaseAndDiacritics(titleNoWhitespace, searchTextNoWhitespace);
        });
        if (artistOrTitleContainsMatch != null)
        {
            songRouletteControl.SelectEntry(artistOrTitleContainsMatch);
        }
    }

    private SingSceneData CreateSingSceneDataWithGivenSongAndSettings(SongMeta songMeta, bool startPaused)
    {
        SingSceneData singSceneData = new();
        singSceneData.SongMetas = new List<SongMeta> { songMeta };
        singSceneData.SingScenePlayerData = CreateSingScenePlayerData();
        singSceneData.partyModeSceneData = sceneData.partyModeSceneData;
        singSceneData.gameRoundSettings = new(nonPersistentSettings.GameRoundSettings);
        singSceneData.StartPaused = startPaused;

        if (singSceneData.gameRoundSettings != null
            && singSceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is ShortSongGameRoundModifier))
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
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_noPlayerSelected_title));
            return null;
        }
        singScenePlayerData.SelectedPlayerProfiles = selectedPlayerProfiles;
        singScenePlayerData.PlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();
        singScenePlayerData.PlayerProfileToVoiceIdMap = playerListControl.GetSelectedPlayerProfileToExtendedVoiceIdMap();
        return singScenePlayerData;
    }

    private void StartSingSceneWithNextSongQueueEntry()
    {
        if (songQueueManager.IsSongQueueEmpty)
        {
            return;
        }

        SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(sceneData.partyModeSceneData);
        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }

    public void StartSingSceneWithGivenSongAndSettings(SongMeta songMeta, bool startPaused, bool fireBeforeSongStartedEvent)
    {
        if (SongMetaUtils.HasFailedToLoadVoices(songMeta))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return;
        }

        if (fireBeforeSongStartedEvent)
        {
            BeforeSongStartedEvent evt = new(songMeta);
            beforeSongStartedEventStream.OnNext(evt);
            if (!evt.CancelReason.IsNullOrEmpty())
            {
                Log.Debug(() => $"Not starting song: {evt.CancelReason}");
                return;
            }
        }

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta, startPaused);
        if (singSceneData == null)
        {
            return;
        }

        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        if (HasPartyModeSceneData)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.partyMode_error_notAvailable));
            return;
        }

        if (SongMetaUtils.HasFailedToLoadVoices(songMeta))
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error));
            return;
        }

        SongEditorSceneData editorSceneData = new();
        editorSceneData.SongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta, false);
        if (singSceneData != null)
        {
            editorSceneData.PlayerProfileToMicProfileMap = singSceneData.SingScenePlayerData.PlayerProfileToMicProfileMap;
            editorSceneData.SelectedPlayerProfiles = singSceneData.SingScenePlayerData.SelectedPlayerProfiles;
        }
        editorSceneData.PreviousSceneData = sceneData;
        editorSceneData.PreviousScene = EScene.SongSelectScene;

        sceneNavigator.LoadScene(EScene.SongEditorScene, editorSceneData);
    }

    public void SelectRandomSong()
    {
        List<SongMeta> availableSongMetas = songRouletteControl.Entries
            .Where(entry => entry is SongSelectSongEntry)
            .Select(entry => (entry as SongSelectSongEntry).SongMeta)
            .Except(new List<SongMeta> { SelectedSong })
            .ToList();
        SongMeta randomSongMeta = RandomUtils.RandomOf(availableSongMetas);
        if (randomSongMeta == null)
        {
            return;
        }
        songRouletteControl.SelectEntryBySongMeta(randomSongMeta);
    }

    private void CheckAudioThenStartSingScene(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        // Check that there is associated sing-along data. If not, ask to open song editor.
        if (!SongMetaUtils.HasSingAlongData(songMeta))
        {
            if (SongMetaUtils.HasFailedToLoadVoices(songMeta))
            {
                ShowFailedToLoadVoicesDialog(songMeta);
                return;
            }

            ShowAskToCreateSingAlongDataDialog(songMeta);
            return;
        }

        // Check that the audio file exists
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            string audioUri = SongMetaUtils.GetAudioUri(songMeta);
            Debug.Log($"");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_error_audioNotFound,
                "name", audioUri));
            return;
        }

        // Check that the used audio format can be loaded.
        songAudioPlayer.LoadAndPlayAsObservable(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError( $"Failed to load audio '{songMeta.GetArtistDashTitle()}': {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_error_audioFailedToLoad,
                    "name", songMeta.Audio,
                    "supportedFormats", ApplicationUtils.supportedAudioFiles.JoinWith(", ")));
            })
            .Subscribe(_ => StartSingSceneWithGivenSongAndSettings(songMeta, false, true));
    }

    private void ShowFailedToLoadVoicesDialog(SongMeta songMeta)
    {
        Translation errorMessage;
        if (songMeta is LazyLoadedVoicesSongMeta lazyLoadedVoicesSongMeta
            && !lazyLoadedVoicesSongMeta.FailedToLoadVoicesExceptionMessage.IsNullOrEmpty())
        {
            errorMessage = Translation.Of(lazyLoadedVoicesSongMeta.FailedToLoadVoicesExceptionMessage);
        }
        else
        {
            errorMessage = Translation.Empty;
        }

        uiManager.CreateErrorInfoDialogControl(
            Translation.Get(R.Messages.songSelectScene_failedToLoadSongDialog_title),
            Translation.Get(R.Messages.songSelectScene_failedToLoadSongDialog_message),
            errorMessage);
    }

    private void ShowAskToCreateSingAlongDataDialog(SongMeta songMeta)
    {
        noSingAlongDataDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_title));
        noSingAlongDataDialogControl.Message = Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_message);
        noSingAlongDataDialogControl.MessageElement.AddToClassList("my-2");
        Button defaultButton = noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_noSingAlongDataDialog_createSingAlongData), _ =>
        {
            noSingAlongDataDialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, true);
        });
        noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.action_openSongEditor), _ =>
        {
            noSingAlongDataDialogControl.CloseDialog();
            StartSongEditorScene(songMeta);
        });
        // noSingAlongDataDialogControl.AddButton("Start anyway", _ => StartSingScene(songMeta));
        noSingAlongDataDialogControl.AddButton(Translation.Get(R.Messages.action_cancel), _ => noSingAlongDataDialogControl.CloseDialog());
        defaultButton.Focus();
    }

    public void AttemptStartSelectedEntry()
    {
        AttemptStartEntry(songRouletteControl.SelectedEntry);
    }

    public void AttemptStartEntry(SongSelectEntry entry)
    {
        if (entry is SongSelectSongEntry songEntry)
        {
            AttemptStartSong(songEntry.SongMeta);
        }

        if (entry is SongSelectFolderEntry folderEntry)
        {
            NavigateIntoFolder(folderEntry.DirectoryInfo);
        }
    }

    private void NavigateIntoFolder(DirectoryInfo directoryInfo)
    {
        if (!settings.NavigateByFoldersInSongSelect)
        {
            return;
        }
        nonPersistentSettings.SongSelectDirectoryInfo = directoryInfo;
        UpdateFilteredSongs();
    }

    public bool TryNavigateToParentFolder()
    {
        if (!settings.NavigateByFoldersInSongSelect)
        {
            return false;
        }

        DirectoryInfo oldDirectoryInfo = nonPersistentSettings.SongSelectDirectoryInfo;
        if (oldDirectoryInfo == null
            || oldDirectoryInfo.Parent == null
            || oldDirectoryInfo.Name == VirtualRootFolderName)
        {
            return false;
        }

        // Select virtual root folder when configured song folder reached
        if (settings.SongDirs.AnyMatch(songFolder =>
                !settings.DisabledSongFolders.Contains(songFolder)
                && new DirectoryInfo(songFolder).FullName == oldDirectoryInfo.FullName))
        {
            nonPersistentSettings.SongSelectDirectoryInfo = new DirectoryInfo(VirtualRootFolderName);
        }
        else
        {
            nonPersistentSettings.SongSelectDirectoryInfo = oldDirectoryInfo.Parent;
        }

        UpdateFilteredSongs();

        // Select entry of previous folder
        SongSelectEntry entryOfOldFolder = songRouletteControl.Entries
            .FirstOrDefault(entry => entry is SongSelectFolderEntry folderEntry
                                     && folderEntry.DirectoryInfo?.FullName == oldDirectoryInfo.FullName);
        if (entryOfOldFolder != null)
        {
            songRouletteControl.SelectEntry(entryOfOldFolder);
        }

        return true;
    }

    private void AttemptStartSong(SongMeta songMeta, bool ignoreRandomlySelectedSong = false, bool ignoreMissingMicProfiles = false)
    {
        List<PlayerProfile> selectedPlayerProfiles = playerListControl.GetSelectedPlayerProfiles();
        Dictionary<PlayerProfile, MicProfile> selectedPlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();

        // Check that any player is selected
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.songSelectScene_noPlayerSelected_message));
            return;
        }

        // Ask to use joker if the user selected a different song than the randomly selected.
        if (!ignoreRandomlySelectedSong
            && IsPartyModeRandomSongSelection
            && partyModeControl.RandomlySelectedSong != songMeta)
        {
            // The user selected a different song than the randomly selected.
            // Ask to use joker or quit.
            if (CanUseSongSelectionJoker)
            {
                partyModeControl.OpenAskToUseJokerDialog(
                    songMeta,
                    () => AttemptStartSong(songMeta, true, ignoreMissingMicProfiles));
            }
            else
            {
                // No jokers left, go back to randomly selected song
                ShowCannotUseJokerMessage();
                songRouletteControl.SelectEntryBySongMeta(partyModeControl.RandomlySelectedSong);
            }
            return;
        }

        // Ask to connect a Companion App when there are players without mics.
        List<PlayerProfile> playerProfilesWithoutMics = selectedPlayerProfiles
            .Where(selectedPlayerProfile => CommonOnlineMultiplayerUtils.IsLocalPlayerProfile(selectedPlayerProfile))
            .Where(selectedPlayerProfile =>
                !selectedPlayerProfileToMicProfileMap.TryGetValue(selectedPlayerProfile, out MicProfile micProfile)
                || micProfile == null)
            .ToList();
        if (!ignoreMissingMicProfiles
            && !playerProfilesWithoutMics.IsNullOrEmpty())
        {
            OpenAskToAssignMicsDialog(
                playerProfilesWithoutMics,
                () => AttemptStartSong(songMeta, ignoreRandomlySelectedSong, true));
            return;
        }

        CheckAudioThenStartSingScene(songMeta);
    }

    private void OpenAskToAssignMicsDialog(List<PlayerProfile> playerProfilesWithoutMics, Action onIgnoreAndStart)
    {
        CloseAskToAssignMicsDialog();

        askToAssignMicsDialog = uiManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_missingMicDialog_title));
        string playerNamesCsv = playerProfilesWithoutMics
            .Select(it => it.Name)
            .JoinWith(", ");
        askToAssignMicsDialog.Message = Translation.Get(R.Messages.songSelectScene_missingMicDialog_message,
            "playerNames", playerNamesCsv);
        askToAssignMicsDialog.AddButton(Translation.Get(R.Messages.songSelectScene_missingMicDialog_ignoreAndStart), _ =>
        {
            CloseAskToAssignMicsDialog();
            onIgnoreAndStart?.Invoke();
        });
        askToAssignMicsDialog.AddButton(Translation.Get(R.Messages.action_cancel), _ =>
        {
            CloseAskToAssignMicsDialog();
        });
    }

    private void CloseAskToAssignMicsDialog()
    {
        if (askToAssignMicsDialog == null)
        {
            return;
        }

        askToAssignMicsDialog.CloseDialog();
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
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.OnSearchTextChanged");

        StartSongRepositorySearch();

        string rawSearchText = songSearchControl.GetRawSearchText();
        if (!rawSearchText.IsNullOrEmpty()
            && lastRawSearchText.IsNullOrEmpty())
        {
            // Remember selection from before search
            selectedEntryBeforeSearch = songRouletteControl.SelectedEntry;
        }
        lastRawSearchText = rawSearchText;

        if (TryExecuteSpecialSearchSyntax(rawSearchText))
        {
            // Special search syntax used. Do not perform normal filtering.
            return;
        }
        UpdateFilteredSongs();

        if (rawSearchText.IsNullOrEmpty())
        {
            // Restore selection from before search
            if (selectedEntryBeforeSearch != null)
            {
                songRouletteControl.SelectEntry(selectedEntryBeforeSearch);
            }
        }
    }

    private void StartSongRepositorySearch()
    {
        IsSongRepositorySearchRunning.Value = true;
        SongRepositorySearchParameters searchParameters = new(songSearchControl.GetSearchText());
        SongRepositoryUtils.SearchSongs(searchParameters)
            .Buffer(TimeSpan.FromMilliseconds(500))
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
            })
            .DoOnCompleted(() => IsSongRepositorySearchRunning.Value = false)
            .Subscribe(songSearchResultEntries =>
            {
                songSearchResultEntries.ForEach(entry => AddSearchResultEntryToSongMetaManager(entry));
                UpdateFilteredSongs();
            });
    }

    private void AddSearchResultEntryToSongMetaManager(SongRepositorySearchResultEntry searchResultEntry)
    {
        try
        {
            SongMeta songMeta = searchResultEntry.SongMeta;
            List<SongIssue> songIssues = searchResultEntry.SongIssues;
            if (songMeta != null
                && !songMetas.Contains(songMeta)
                && !songMetaManager.ContainsSongMeta(songMeta))
            {
                songMetas.Add(songMeta);
                songMetaManager.AddSongMeta(songMeta);
                songIssueManager.AddSongIssues(songIssues);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.Log($"Failed to add search result entry '{searchResultEntry.SongMeta.GetArtistDashTitle()}'");
        }
    }

    private List<SongMeta> GetFilteredSongMetas()
    {
        // Ignore prefix for special search syntax
        IPlaylist playlist = SongSelectionPlaylistChooserControl.Selection.Value;
        List<SongMeta> filteredSongs = songSearchControl.GetFilteredSongMetas(songMetas)
            .Where(songMeta => playlist == null
                            || playlist.HasSongEntry(songMeta))
            .Where(songMeta => songSelectFilterControl.SongMetaPassesActiveFilters(songMeta))
            .Where(songMeta => nonPersistentSettings.SongSelectDirectoryInfo == null
                               // Typically each song has its own folder. Thus, show a song if its PARENT folder matches the selected folder.
                               || SongMetaUtils.GetDirectoryInfo(songMeta)?.Parent?.FullName == nonPersistentSettings.SongSelectDirectoryInfo.FullName)
            .OrderBy(songMeta => GetSongMetaOrderByProperty(songMeta), songMetaPropertyComparer)
            .ToList();
        return filteredSongs;
    }

    private object GetSongMetaOrderByProperty(SongMeta songMeta)
    {
        switch (settings.SongOrder)
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
            case ESongOrder.LocalHighScore:
                // Return negative value to sort descending
                return -StatisticsUtils.GetLocalHighScore(statistics, songMeta, settings.Difficulty);
            case ESongOrder.CreationTime:
                return songMeta.FileInfo == null
                    ? 0
                    : -songMeta.FileInfo.CreationTimeUtc.Ticks;
            case ESongOrder.LastModificationTime:
                return songMeta.FileInfo == null
                    ? 0
                    : -songMeta.FileInfo.LastWriteTimeUtc.Ticks;
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
        bb.BindExistingInstance(songSearchControl);
        bb.BindExistingInstance(songSelectSelectedSongDetailsControl);
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

    private void UpdateFilteredSongs()
    {
        if (!songMetaManager.IsSongScanFinished)
        {
            return;
        }

        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.UpdateFilteredSongs");

        if (settings.NavigateByFoldersInSongSelect
            && nonPersistentSettings.SongSelectDirectoryInfo == null)
        {
            nonPersistentSettings.SongSelectDirectoryInfo = new DirectoryInfo(VirtualRootFolderName);
        }
        else if (!settings.NavigateByFoldersInSongSelect
                 && nonPersistentSettings.SongSelectDirectoryInfo != null)
        {
            nonPersistentSettings.SongSelectDirectoryInfo = null;
        }

        List<SongMeta> filteredSongMetas = GetFilteredSongMetas();
        if (!filteredSongMetas.IsNullOrEmpty()
            && filteredSongMetas.SequenceEqual(lastSongMetasOfSongRouletteControl)
            && lastDirectoryInfoOfSongRouletteControl == nonPersistentSettings.SongSelectDirectoryInfo)
        {
            return;
        }
        lastSongMetasOfSongRouletteControl = filteredSongMetas;
        lastDirectoryInfoOfSongRouletteControl = nonPersistentSettings.SongSelectDirectoryInfo;

        List<SongSelectEntry> newEntries = new();

        // Add folder entries
        if (settings.NavigateByFoldersInSongSelect)
        {
            List<DirectoryInfo> directoryInfos = GetFilteredDirectoryInfos();
            newEntries.AddRange(directoryInfos
                .Select(directoryInfo => new SongSelectFolderEntry(directoryInfo)));
        }

        // Add song entries
        newEntries.AddRange(filteredSongMetas
            .Select(songMeta => new SongSelectSongEntry(songMeta) as SongSelectEntry));

        songRouletteControl.SetEntries(newEntries);
    }

    private List<DirectoryInfo> GetFilteredDirectoryInfos()
    {
        if (nonPersistentSettings.SongSelectDirectoryInfo == null)
        {
            return new();
        }

        if (nonPersistentSettings.SongSelectDirectoryInfo.Name == VirtualRootFolderName)
        {
            return settings.SongDirs
                .Where(songFolder => !settings.DisabledSongFolders.Contains(songFolder))
                .Select(songFolder => new DirectoryInfo(songFolder))
                .ToList();
        }

        // Typically each song has its own folder.
        // Thus, songs are shown if the PARENT folder matches the selected folder. This moves the songs one level up.
        // As a result, a folder should only be shown when it has further subfolders.
        List<DirectoryInfo> directoryInfos = nonPersistentSettings.SongSelectDirectoryInfo
            .EnumerateDirectories()
            .Where(childDirectoryInfo => childDirectoryInfo.EnumerateDirectories().Any())
            .ToList();

        string searchText = songSearchControl.GetSearchText();
        if (searchText.IsNullOrEmpty())
        {
            return directoryInfos;
        }

        return directoryInfos
            .Where(directoryInfo => directoryInfo.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
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
                songRouletteControl.SelectEntryByIndex(number - 1, false);
                return true;
            }
        }
        return false;
    }

    public void UpdateTranslation()
    {
        if (HasPartyModeSceneData)
        {
            sceneTitle.SetTranslatedText(Translation.Of(
                $"{Translation.Get(R.Messages.songSelectScene_title)}\n"
                + $"{PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.RoundCount}"));
        }
    }

    private void OnSubmitSongRoulette()
    {
        AttemptStartSelectedEntry();
    }

    private void OnSubmitSearch()
    {
        // Continue browsing songs from the currently selected entry.
        selectedEntryBeforeSearch = songRouletteControl.SelectedEntry;
        songSearchControl.ResetSearchText();
        songRouletteControl.Focus();
    }

    public void OnCancelSearch()
    {
        songSearchControl.ResetSearchText();
        songRouletteControl.Focus();
    }

    public void ShowCannotUseJokerMessage()
    {
        NotificationManager.CreateNotification(Translation.Get(R.Messages.partyMode_error_noJokers));
    }

    public List<PlayerProfile> GetEnabledPlayerProfiles()
    {
        if (!HasPartyModeSceneData)
        {
            return SettingsUtils.GetPlayerProfiles(settings, nonPersistentSettings)
                .Where(playerProfile => playerProfile.IsEnabled)
                .ToList();
        }
        else if (PartyModeSettings.TeamSettings.IsFreeForAll)
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
            PartyModeSettings.TeamSettings.Teams
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

    public void AskToRecreateSingAlongData(SongMeta songMeta)
    {
        MessageDialogControl dialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_title, "songName", songMeta.Title));
        dialogControl.Message = Translation.Get(R.Messages.songSelectScene_recreateSongDialog_message);
        dialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_recreateAndSave), evt =>
        {
            dialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, true);
        });
        dialogControl.AddButton(Translation.Get(R.Messages.songSelectScene_recreateSongDialog_recreateButDoNotSave), evt =>
        {
            dialogControl.CloseDialog();
            createSingAlongSongControl.CreateSingAlongSong(songMeta, false);
        });
        dialogControl.AddButton(Translation.Get(R.Messages.action_cancel), evt =>
        {
            dialogControl.CloseDialog();
        });

        dialogControl.AddInformationMessage($"AI model parameters can be changed in the song editor");
    }
}
