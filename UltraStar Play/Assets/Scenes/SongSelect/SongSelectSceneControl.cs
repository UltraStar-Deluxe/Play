using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ProTrans;
using UniInject;
using UniRx;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneControl : MonoBehaviour, INeedInjection, IBinder, ITranslator, IInjectionFinishedListener
{
    private readonly IComparer<object> songMetaPropertyComparer = new NullOrEmptyValueLastComparer();

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
    
    [Inject]
    private AchievementEventStream achievementEventStream;
    
    [Inject]
    private SongSelectSceneData sceneData;

    private List<SongMeta> songMetas;
    private float lastSongMetaCountUpdateTimeInSeconds;
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
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private SongMetaManager songMetaManager;
    
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
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.passTheMicToggle)]
    private Toggle passTheMicToggle;
    
    private readonly SongSearchControl songSearchControl = new();

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

    static readonly ProfilerMarker onInjectionFinishedProfilerMarker = new ProfilerMarker("SongSelectSceneControl.OnInjectionFinished");
    
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
        
        songMetaManager.ScanFilesIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!SongMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        songMetaManager.SongScanFinishedEventStream
            .ObserveOnMainThread()
            .Subscribe(evt =>
            {
                InitSongMetas();
                UpdateFilteredSongs();
                UpdateSongScanLabels(SongMetaManager.IsSongScanFinished || evt != null);
            })
            .AddTo(gameObject);
        UpdateSongScanLabels(SongMetaManager.IsSongScanFinished);
        
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
        showSearchExpressionInfoButton.RegisterCallbackButtonTriggered(_ => ShowSearchExpressionHelpDialog());
        
        fuzzySearchTextLabel.ShowByDisplay();
        songSelectSceneInputControl.FuzzySearchText
            .Subscribe(newValue => fuzzySearchTextLabel.text = newValue);

        songRouletteControl.SubmitEventStream.Subscribe(_ => AttemptStartSelectedSong());
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
                if (songOrderDropdownField.value is ESongOrder.Highscore)
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

        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream
            .Subscribe(_ => UpdateInputLegend())
            .AddTo(gameObject);

        importSongsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.SongLibraryOptionsScene)));

        createSingAlongSongControl.CreatedSingAlongVersionEventStream.Subscribe(processedSongMeta =>
        {
            UiManager.CreateNotification($"Created sing-along version of '{Path.GetFileName(processedSongMeta.Mp3)}'");
        });

        // Song queue
        InitSongQueue();
        
        // Init modifier dialog
        InitModifierDialog();
        
        // Hide slide-in controls with click outside
        InitHideSlideInControlsViaClick();
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
        modifierDialogControl.GetAvailableModifiersFunction = GetAvailableModifiers;

        // Modifier active icon
        modifiersActiveIcon.HideByDisplay();
        nonPersistentSettings.ObserveEveryValueChanged(it => it.GameRoundSettings.AnyModifierOrFinishConditionActive)
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
        
        // Disable 'pass the mic' toggle if needed. It requires a team with at least 2 players
        if (!HasPartyModeSceneData
            || PartyModeSettings.TeamSettings.Teams.AllMatch(team =>
                team.playerProfiles.Count + team.guestPlayerProfiles.Count <= 1))
        {
            passTheMicToggle.value = false;
            passTheMicToggle.SetEnabled(false);
            passTheMicToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UiManager.CreateNotification("'Pass the mic' requires a team with more than one player");
                    StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => passTheMicToggle.value = false));
                }
            });
        }
        else
        {
            passTheMicToggle.SetEnabled(true);
        }
    }

    private List<EGameRoundModifier> GetAvailableModifiers()
    {
        List<EGameRoundModifier> availableModifiers = EnumUtils.GetValuesAsList<EGameRoundModifier>();
        if (!HasPartyModeSceneData)
        {
            availableModifiers.Remove(EGameRoundModifier.PassTheMic);
        }
        return availableModifiers;
    }
    
    private void UpdateSongQueue()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.UpdateSongQueue");
        
        string newSongQueueLengthAsString = SongQueueManager.SongQueueLength.ToString();
        if (songQueueLengthLabel.text != newSongQueueLengthAsString)
        {
            songQueueLengthContainer.SetVisibleByDisplay(SongQueueManager.SongQueueLength > 0);
            songQueueLengthLabel.text = newSongQueueLengthAsString;
            LeanTween.value(gameObject, Vector3.one * 1.5f, Vector3.one, 1.5f)
                .setEaseOutBounce()
                .setOnUpdate(s => songQueueLengthLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s))));
        }
        songQueueUiControl.SetSongQueueEntryDtos(songQueueManager.GetSongQueueEntries());
        startSongQueueButton.SetEnabled(!songQueueManager.IsSongQueueEmpty);
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(songQueueOverlay);
    }

    private void UpdateModifiersActiveIcon()
    {
        modifiersActiveIcon.SetVisibleByDisplay(nonPersistentSettings.GameRoundSettings.AnyModifierOrFinishConditionActive);
        modifiersInactiveIcon.SetVisibleByDisplay(!nonPersistentSettings.GameRoundSettings.AnyModifierOrFinishConditionActive);
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
            .Subscribe(newValue => settings.PlayerProfiles.ForEach(it => it.Difficulty = newValue));

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
            currentDifficultyLabel.text = "No Scores";
        }
        else
        {
            currentDifficultyLabel.text = settings.Difficulty.ToString();
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
        songQueueEntryDto.GameRoundSettings = new(nonPersistentSettings.GameRoundSettings);
        return songQueueEntryDto;
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

        lyricsDialogControl = uiManager.CreateDialogControl($"{songMeta.Title}");
        lyricsDialogControl.DialogClosedEventStream.Subscribe(_ => lyricsDialogControl = null);
        
        Label CreateLyricsLabel(string lyrics)
        {
            Label lyricsLabel = new Label(lyrics);
            lyricsLabel.enableRichText = true;
            lyricsLabel.AddToClassList("songSelectLyricsPreview");
            return lyricsLabel;
        }
        
        if (songMeta.GetVoices().Count < 2)
        {
            string lyrics = SongMetaUtils.GetLyrics(songMeta, Voice.firstVoiceName);
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(lyrics));
        }
        else
        {
            string firstVoiceLyrics = $"<i><b>{songMeta.VoiceNames.FirstOrDefault().Value}</b></i>\n\n" 
                                      + SongMetaUtils.GetLyrics(songMeta, Voice.firstVoiceName);
            string secondVoiceLyrics = $"<i><b>{songMeta.VoiceNames.LastOrDefault().Value}</b></i>\n\n" 
                                       + SongMetaUtils.GetLyrics(songMeta, Voice.secondVoiceName);
            
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(firstVoiceLyrics));
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(secondVoiceLyrics));
        }
        
        // Add attribution and license info
        AccordionItem attributionAccordionItem = new("Attribution");
        attributionAccordionItem.Add(AttributionUtils.CreateAttributionVisualElement(songMeta));
        lyricsDialogControl.AddVisualElement(attributionAccordionItem);
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(lyricsDialogControl.DialogRootVisualElement);
    }

    public void InitSongMetas()
    {
        if (!SongMetaManager.IsSongScanFinished)
        {
            return;
        }
        
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongMetas");
        
        songMetas = new List<SongMeta>(songMetaManager.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        
        // Trigger achievement
        if (songMetas.Count > 100)
        {
            achievementEventStream.OnNext(AchievementId.browseMoreThan100Songs);
        }
    }

    private void UpdateSongScanLabels(bool isSongScanFinished)
    {
        if (SongMetaManager.LoadedSongsCount > 0)
        {
            songScanInProgressProgressLabel.text = $"{SongMetaManager.LoadedSongsPercent:00} %";
        }
        
        if (isSongScanFinished)
        {
            songScanInProgressContainer.HideByDisplay();
            noSongsFoundContainer.SetVisibleByDisplay(SongMetaManager.LoadedSongsCount <= 0);
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
        if (!SongMetaManager.IsSongScanFinished
            && Time.time - lastSongMetaCountUpdateTimeInSeconds > 1f )
        {
            lastSongMetaCountUpdateTimeInSeconds = Time.time;
            UpdateSongScanLabels(SongMetaManager.IsSongScanFinished);
        }
    }

    private void InitSongRoulette()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectScene.InitSongRouletteSongMetas");
        
        UpdateFilteredSongs();
        songRouletteControl.Selection.Subscribe(newValue => songSelectSelectedSongDetailsControl.OnSongSelectionChanged(newValue));
        songRouletteControl.SelectionClickedEventStream
            .Subscribe(_ => AttemptStartSelectedSong());

        if (sceneData.SongMeta != null)
        {
            songRouletteControl.SelectSong(sceneData.SongMeta);
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

    private SingSceneData CreateSingSceneDataWithGivenSongAndSettings(SongMeta songMeta)
    {
        SingSceneData singSceneData = new();
        singSceneData.SongMetas = new List<SongMeta> { songMeta };
        singSceneData.SingScenePlayerData = CreateSingScenePlayerData();
        singSceneData.partyModeSceneData = sceneData.partyModeSceneData;
        singSceneData.gameRoundSettings = new(nonPersistentSettings.GameRoundSettings);

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

    private void StartSingScene(SongMeta songMeta)
    {
        StartSingSceneWithGivenSongAndSettings(songMeta);
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

    private void StartSingSceneWithGivenSongAndSettings(SongMeta songMeta)
    {
        if (songMeta.FailedToLoadVoices)
        {
            UiManager.CreateNotification("Failed to load song. Check log for details.");
            return;
        }

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta);
        if (singSceneData != null)
        {
            sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
        }
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        if (HasPartyModeSceneData)
        {
            UiManager.CreateNotification("Song editor not available in Team & Tournament mode.");
            return;
        }

        if (songMeta.FailedToLoadVoices)
        {
            UiManager.CreateNotification("Failed to load song. Check log for details.");
            return;
        }

        SongEditorSceneData editorSceneData = new();
        editorSceneData.SongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneDataWithGivenSongAndSettings(songMeta);
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
        List<SongMeta> availableSongMetas = songRouletteControl.Songs
            .Except(new List<SongMeta> { SelectedSong })
            .ToList();
        SongMeta randomSongMeta = RandomUtils.RandomOf(availableSongMetas);
        if (randomSongMeta == null)
        {
            return;
        }
        songRouletteControl.SelectSong(randomSongMeta);
    }

    private void CheckAudioThenStartSingScene(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        // Check that there is associated sing-along data. If not, ask to open song editor.
        if (SongMetaUtils.IsGeneratedAndNotYetSaved(songMeta)
            && SongMetaUtils.GetAllNotes(songMeta).IsNullOrEmpty())
        {
            noSingAlongDataDialogControl = uiManager.CreateDialogControl("No Sing-Along Data");
            noSingAlongDataDialogControl.Message = "This song does not yet have associated sing-along data.\n"
                                                   + "Do you want to open the song editor?";
            noSingAlongDataDialogControl.MessageElement.AddToClassList("my-2");
            Button openSongEditorButton =
                noSingAlongDataDialogControl.AddButton("Open Song Editor", _ => StartSongEditorScene(songMeta));
            noSingAlongDataDialogControl.AddButton("Start Song", _ => StartSingScene(songMeta));
            noSingAlongDataDialogControl.AddButton("Cancel", _ => noSingAlongDataDialogControl.CloseDialog());
            openSongEditorButton.Focus();
            return;
        }

        // Check that the audio file exists
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            string audioUri = SongMetaUtils.GetAudioUri(songMeta);
            string message = "Audio file resource does not exist: " + audioUri;
            Debug.Log(message);
            UiManager.CreateNotification(message);
            return;
        }

        // Check that the used audio format can be loaded.
        songAudioPlayer.LoadSongAudio(songMeta)
            .CatchIgnore((Exception error) =>
            {
                string message = $"Audio file '{songMeta.Mp3}' could not be loaded.\n" +
                                 $"Please use one of {ApplicationUtils.supportedAudioFiles.ToCsv(",", "", "")}\n" +
                                 $"or a supported website URI.";
                Debug.Log(message);
                UiManager.CreateNotification(message);
            })
            .Subscribe(_ => StartSingScene(songMeta));
    }

    public void AttemptStartSelectedSong()
    {
        AttemptStartSong(songRouletteControl.SelectedSongEntryControl.SongMeta);
    }
    
    public void AttemptStartSong(SongMeta songMeta, bool ignoreRandomlySelectedSong = false, bool ignoreMissingMicProfiles = false)
    {
        List<PlayerProfile> selectedPlayerProfiles = playerListControl.GetSelectedPlayerProfiles();
        Dictionary<PlayerProfile, MicProfile> selectedPlayerProfileToMicProfileMap = playerListControl.GetSelectedPlayerProfileToMicProfileMap();
        
        // Check that any player is selected
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            UiManager.CreateNotification(
                TranslationManager.GetTranslation(R.Messages.songSelectScene_noPlayerSelected_message));
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
                songRouletteControl.SelectSong(partyModeControl.RandomlySelectedSong);
            }
            return;
        }

        // Ask to connect a Companion App when there are players without mics.
        List<PlayerProfile> playerProfilesWithoutMics = selectedPlayerProfiles
            .Where(selectedPlayerProfile => !selectedPlayerProfileToMicProfileMap.TryGetValue(selectedPlayerProfile, out MicProfile micProfile) 
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
        
        askToAssignMicsDialog = uiManager.CreateDialogControl("Missing Microphones");
        string playerNamesCsv = playerProfilesWithoutMics
            .Select(it => it.Name)
            .ToCsv(", ", "", "");
        askToAssignMicsDialog.Message = $"Missing microphones for player(s): {playerNamesCsv}\n" +
                                        $"Connect a Companion app or assign another microphone.\n";
        askToAssignMicsDialog.AddButton("Start anyway", _ =>
        {
            CloseAskToAssignMicsDialog();
            onIgnoreAndStart?.Invoke();
        });
        askToAssignMicsDialog.AddButton(TranslationManager.GetTranslation(R.Messages.cancel), _ =>
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
            .OrderBy(songMeta => GetSongMetaOrderByProperty(songMeta), songMetaPropertyComparer)
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
            case ESongOrder.Highscore:
                // Return negative value to sort descending
                return -statistics.GetLocalHighscore(songMeta, settings.Difficulty);
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

    public void UpdateFilteredSongs()
    {
        if (!SongMetaManager.IsSongScanFinished)
        {
            return;
        }
        
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSceneControl.UpdateFilteredSongs");
        
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
            sceneTitle.text += $"\n{PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.RoundCount}";
        }

        songSearchControl.UpdateTranslation();
        UpdateInputLegend();
    }

    public void OnSubmitSearch()
    {
        selectedSongBeforeSearch = SelectedSong;
        songSearchControl.ResetSearchText();
        songRouletteControl.Focus();
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
}
