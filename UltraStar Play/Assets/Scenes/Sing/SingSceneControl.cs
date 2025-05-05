using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneControl : MonoBehaviour, INeedInjection, IBinder, IInjectionFinishedListener
{
    private static SingSceneControl instance;
    public static SingSceneControl Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SingSceneControl>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        completedSongCountSinceAppStart = 0;
    }
    private static int completedSongCountSinceAppStart;

    [InjectedInInspector]
    public PlayerControl playerControlPrefab;

    [InjectedInInspector]
    public VisualTreeAsset playerUi;

    [InjectedInInspector]
    public VisualTreeAsset playerInfoUi;

    [InjectedInInspector]
    public VisualTreeAsset sentenceRatingUi;

    [InjectedInInspector]
    public VisualTreeAsset noteUi;

    [InjectedInInspector]
    public VisualTreeAsset perfectEffectStarUi;

    [InjectedInInspector]
    public VisualTreeAsset goldenNoteStarUi;

    [InjectedInInspector]
    public VisualTreeAsset goldenNoteHitStarUi;

    [Inject(UxmlName = R.UxmlNames.background)]
    public VisualElement background;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SingSceneWebcamControl webcamControl;

    [InjectedInInspector]
    public SingSceneAlternativeAudioPlayer alternativeAudioPlayer;

    [InjectedInInspector]
    public SingSceneFinisher singSceneFinisher;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private PlayerProfileImageManager playerProfileImageManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private Statistics statistics;

    [Inject]
    private SteamManager steamManager;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject(UxmlName = R.UxmlNames.topLyricsContainer)]
    private VisualElement topLyricsContainer;

    [Inject(UxmlName = R.UxmlNames.bottomLyricsContainer)]
    private VisualElement bottomLyricsContainer;

    [Inject(UxmlName = R.UxmlNames.playerUiContainer)]
    private VisualElement playerUiContainer;

    [Inject(UxmlName = R.UxmlNames.playerUiContainerPlaceholder)]
    private VisualElement playerUiContainerPlaceholder;

    [Inject(UxmlName = R.UxmlNames.songTimeProgressBar)]
    private ProgressBar songTimeProgressBar;

    [Inject(UxmlName = R.UxmlNames.detailedTimeBar)]
    private VisualElement detailedTimeBar;

    [Inject(UxmlName = R.UxmlNames.governanceOverlayDetailedTimeBar)]
    private VisualElement governanceOverlayDetailedTimeBar;

    [Inject(UxmlClass = R.UssClasses.playerInfoUiList)]
    private List<VisualElement> playerInfoUiLists;

    [Inject(UxmlName = R.UxmlNames.passTheMicProgressBar)]
    private VisualElement passTheMicProgressBar;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private DialogManager dialogManager;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private AchievementEventStream achievementEventStream;

    public List<PlayerControl> PlayerControls { get; private set; } = new();

    private PlayerControl lastLeadingPlayerControl;

    private VisualElement[] playerUiColumns;

    public bool IsPaused => !songAudioPlayer.IsPlaying;

    private SingSceneData sceneData;
    public SongMeta SongMeta
    {
        get
        {
            if (sceneData.IsMedley)
            {
                if (sceneData.MedleySongIndex >= sceneData.SongMetas.Count)
                {
                    Debug.LogWarning($"Cannot start medley song at index {sceneData.MedleySongIndex} because there are only {sceneData.SongMetas.Count} songs selected for the medley. Exiting SingScene.");
                    FinishScene(false, false);
                    return null;
                }

                return sceneData.SongMetas[sceneData.MedleySongIndex];
            }

            return sceneData.SongMetas.FirstOrDefault();
        }
    }

    public double DurationInMillis => songAudioPlayer.DurationInMillis;
    public double PositionInMillis => songAudioPlayer.PositionInMillis;
    public double CurrentBeat => songAudioPlayer.GetCurrentBeat(false);

    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSettings PartyModeSettings => sceneData.partyModeSceneData.PartyModeSettings;
    public bool IsPassTheMic => HasPartyModeSceneData &&
                                sceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is PassTheMicGameRoundModifier);

    private readonly List<SingingLyricsControl> singingLyricsControls = new();

    private readonly TimeBarControl timeBarControl = new();
    private readonly TimeBarControl governanceOverlayTimeBarControl = new();

    private MessageDialogControl dialogControl;

    private readonly SingSceneGovernanceControl singSceneGovernanceControl = new();
    private readonly CommonScoreControl commonScoreControl = new();
    private readonly SingSceneCountdownControl countdownControl = new();
    private readonly SingSceneAudioFadeInControl audioFadeInControl = new();
    private readonly SingSceneMedleyControl medleyControl = new();

    // TODO: Should be created like other GameRoundModifiers, in PassTheMicGameRoundModifier.CreateControl
    private readonly PassTheMicControl passTheMicControl = new();

    public bool IsCommonScore => settings.ScoreMode == EScoreMode.CommonAverage
                                 && sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 2;

    public bool IsIndividualScore => settings.ScoreMode == EScoreMode.Individual
                                     || (settings.ScoreMode == EScoreMode.CommonAverage
                                         && sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count <= 1);

    private float startTimeInSeconds;
    private bool hasRecordedSongStartedStatistics;
    private bool hasRecordedSongFinishedStatistics;
    private bool hasRecordedHighScoreStatistics;

    private bool hasFinishedScene;

    public ReactiveProperty<int> ModifiedVolumePercent { get; private set; } = new(100);

    private readonly Subject<CancelableEvent> beforeSkipEventStream = new();
    public IObservable<CancelableEvent> BeforeSkipEventStream => beforeSkipEventStream;

    private readonly Subject<CancelableEvent> beforeRestartEventStream = new();
    public IObservable<CancelableEvent> BeforeRestartEventStream => beforeRestartEventStream;

    private readonly Subject<VoidEvent> restartedEventStream = new();
    public IObservable<VoidEvent> RestartedEventStream => restartedEventStream;

    private readonly Subject<VoidEvent> pausedEventStream = new();
    public IObservable<VoidEvent> PausedEventStream => pausedEventStream;

    private readonly Subject<VoidEvent> unpausedEventStream = new();
    public IObservable<VoidEvent> UnpausedEventStream => unpausedEventStream;

    public void OnInjectionFinished()
    {
        // PassTheMicControl may not be executed.
        // Thus, we hide the progress bar here as a workaround.
        passTheMicProgressBar?.HideByDisplay();

        injector
            .WithRootVisualElement(detailedTimeBar)
            .Inject(timeBarControl);
        injector
            .WithRootVisualElement(governanceOverlayDetailedTimeBar)
            .Inject(governanceOverlayTimeBarControl);
        injector.Inject(commonScoreControl);
        injector.Inject(countdownControl);
        injector.Inject(medleyControl);
        injector.Inject(audioFadeInControl);
    }

    private void Start()
    {
        string playerProfilesCsv = sceneData.SingScenePlayerData.SelectedPlayerProfiles.Select(it => it.Name).JoinWith(", ");
        Debug.Log($"{playerProfilesCsv} start (or continue) singing of {SongMeta.Title} at {sceneData.PositionInMillis} ms.");

        startTimeInSeconds = Time.time;

        injector.Inject(singSceneGovernanceControl);

        // Prepare player UI layout (depends on player count)
        PreparePlayerUiLayout();

        // Create PlayerControl (and PlayerUi) for each player
        List<PlayerProfile> playerProfilesWithoutMic = new();
        for (int i = 0; i < sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count; i++)
        {
            PlayerProfile playerProfile = sceneData.SingScenePlayerData.SelectedPlayerProfiles[i];
            sceneData.SingScenePlayerData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            if (micProfile == null
                && (playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                    || lobbyMemberPlayerProfile.IsLocal))
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }

            PlayerControl playerControl;
            try
            {
                playerControl = CreatePlayerControl(playerProfile, micProfile, i);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to create player control for player '{playerProfile.Name}': {ex.Message}");
                continue;
            }

            if (sceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out List<ISingingResultsPlayerScore> scoreDatas))
            {
                if (sceneData.MedleySongIndex < 0)
                {
                    // No medley, select first score data
                    playerControl.PlayerScoreControl.SetCalculationData(scoreDatas.FirstOrDefault());
                }
                else if (sceneData.MedleySongIndex < scoreDatas.Count)
                {
                    // This is a medley (or short song), select score data for this medley entry song
                    playerControl.PlayerScoreControl.SetCalculationData(scoreDatas[sceneData.MedleySongIndex]);
                }

                playerControl.PlayerUiControl.ShowTotalScore(playerControl.PlayerScoreControl.PlayerScore.TotalScore, false);
            }

            // Update leading player icon
            if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 1)
            {
                playerControl.PlayerMicPitchTracker.SentenceAnalyzedEventStream
                    .Subscribe(_ => UpdateLeadingPlayerIcon());
            }
        }

        AddPlayerUisToUiDocument();

        // Handle dummy singers
        if (Application.isEditor)
        {
            InitDummySingers();
        }

        // Create warning about missing microphones
        if (!playerProfilesWithoutMic.IsNullOrEmpty())
        {
            ShowMissingMicrophonesDialog(playerProfilesWithoutMic);
        }

        webcamControl.InitWebcam();

        InitSingingLyricsControls();

        StartAudioAndVideo();

        // Input legend (in pause overlay)
        UpdateInputLegend();
        inputManager.InputDeviceChangedEventStream.Subscribe(_ => UpdateInputLegend());

        // Progress bar to show time in song
        songTimeProgressBar.value = 0;
        songAudioPlayer.PositionEventStream.Subscribe(_ =>
        {
            double startTagInMillis = SongMeta.StartInMillis;
            double endTagInMillis = SongMeta.EndInMillis;
            double positionInMillisConsideringStartTag = songAudioPlayer.PositionInMillis - startTagInMillis;
            double durationInMillisConsideringStartAndEndTag = songAudioPlayer.DurationInMillis - startTagInMillis - endTagInMillis;
            double progressInPercent = 100 * (positionInMillisConsideringStartTag / durationInMillisConsideringStartAndEndTag);
            songTimeProgressBar.value = (float) progressInPercent;
        });
        settings.ObserveEveryValueChanged(it => it.ShowSongProgressBar)
            .Subscribe(newValue =>
            {
                songTimeProgressBar.SetVisibleByDisplay(newValue is ESongProgressBar.Plain);
                detailedTimeBar.SetVisibleByDisplay(newValue is ESongProgressBar.Detailed);
            });

        // Update TimeBar every second
        AwaitableUtils.ExecuteRepeatedlyInSecondsAsync(gameObject, 1f, () =>
        {
            timeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInMillis, songAudioPlayer.DurationInMillis);
            governanceOverlayTimeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInMillis, songAudioPlayer.DurationInMillis);
        });

        // Start medley if needed
        if (sceneData.IsMedley)
        {
            medleyControl.StartCurrentMedleySong();
        }

        // Instantiate game round modifier controls
        CreateGameRoundModifiers();

        // Set up 'pass the mic' control
        if (sceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is PassTheMicGameRoundModifier))
        {
            injector.Inject(passTheMicControl);
        }

        TriggerAchievementsAtSongStart();
    }

    private async void StartAudioAndVideo()
    {
        await StartAudioAndVideoAsync();
    }

    private async Awaitable StartAudioAndVideoAsync()
    {
        await StartAudioPlaybackAsync();
        StartVideoOrShowBackgroundImage();
    }

    private void CreateGameRoundModifiers()
    {
        List<IGameRoundModifier> modifiers = sceneData.gameRoundSettings.modifiers;
        foreach (IGameRoundModifier modifier in modifiers)
        {
            try
            {
                GameRoundModifierControl modifierControl = modifier.CreateControl();
                if (modifierControl == null)
                {
                    continue;
                }

                try
                {
                    injector.Inject(modifierControl);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to inject control for modifier {modifier}");
                }
                Debug.Log($"Created control for modifier {modifier}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to create control for modifier {modifier}");
            }
        }
    }

    private void TriggerAchievementsAtSongStart()
    {
        // Two players with different lyrics
        if (sceneData.SingScenePlayerData.PlayerProfileToVoiceIdMap.Count == 2
            && sceneData.SingScenePlayerData.PlayerProfileToVoiceIdMap.Values.Distinct().Count() > 1)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.startDuetWithDifferentLyrics));
        }

        if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 4)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.startSongWithFourOrMorePlayers));
        }

        // Medley with at least two entries
        if (sceneData.IsMedley
            && sceneData.MedleySongIndex >= 0
            && sceneData.SongMetas.Count > 1)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.startMedleyWithAtLeastTwoSongs));
        }
    }

    private void ShowMissingMicrophonesDialog(List<PlayerProfile> playerProfilesWithoutMic)
    {
        if (dialogControl != null)
        {
            return;
        }

        string playerNameCsv = playerProfilesWithoutMic
            .Select(it => it.Name)
            .ToList()
            .JoinWith(", ");

        dialogControl = dialogManager.CreateDialogControl(Translation.Get(R.Messages.singScene_missingMicrophones_title));
        dialogControl.DialogClosedEventStream.Subscribe(_ => dialogControl = null);
        dialogControl.Message = Translation.Get(R.Messages.singScene_missingMicrophones_message,
            "playerNames", playerNameCsv);
    }

    public void OnDestroy()
    {
        webcamControl?.Stop();
        singSceneGovernanceControl?.Dispose();
        audioFadeInControl?.Dispose();
    }

    private void InitDummySingers()
    {
        bool includeInactive = false;
        AbstractDummySinger[] findObjectsOfType = FindObjectsOfType<AbstractDummySinger>(includeInactive);
        foreach (AbstractDummySinger dummySinger in findObjectsOfType)
        {
            if (dummySinger.playerIndexToSimulate < PlayerControls.Count)
            {
                dummySinger.SetPlayerControl(PlayerControls[dummySinger.playerIndexToSimulate]);
                injector.Inject(dummySinger);
            }
            else
            {
                Debug.LogWarning("DummySinger cannot simulate player with index " + dummySinger.playerIndexToSimulate);
                dummySinger.gameObject.SetActive(false);
            }
        }
    }

    private void PreparePlayerUiLayout()
    {
        int playerCount = sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count;
        playerUiContainer.Children()
            .Where(it => it.name != R.UxmlNames.commonScoreSentenceRatingContainer)
            .ToList()
            .ForEach(it => it.RemoveFromHierarchy());

        UpdatePlayerUiContainerHeight();

        playerInfoUiLists.ForEach(playerInfoUiList => playerInfoUiList.Clear());
        if (playerCount <= 1)
        {
            // Add empty VisualElement as spacer. Otherwise the player UI would take all the available space.
            VisualElement spacer = new();
            spacer.style.flexGrow = 1;
            playerUiContainer.Add(spacer);
            return;
        }

        if (playerCount > 3)
        {
            // Create row
            playerUiContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            // Create columns
            int columnCount = (int)Math.Sqrt(playerCount);
            playerUiColumns = new VisualElement[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                VisualElement column = new();
                column.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
                column.style.flexGrow = 1;
                column.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

                playerUiContainer.Add(column);
                playerUiColumns[i] = column;
            }
        }
    }

    private void UpdatePlayerUiContainerHeight()
    {
        // The player UI should be in front of the lyrics and player info UIs.
        // Therefor, it uses absolute positioning.
        // Its size is adjusted here by a placeholder element that is positioned relatively.
        Rect placeholderWorldBound = playerUiContainerPlaceholder.worldBound;
        Rect worldBound = playerUiContainer.worldBound;
        if (Math.Abs(placeholderWorldBound.xMin - worldBound.xMin) < 1
            && Math.Abs(placeholderWorldBound.xMax - worldBound.xMax) < 1
            && Math.Abs(placeholderWorldBound.yMin - worldBound.yMin) < 1
            && Math.Abs(placeholderWorldBound.yMax - worldBound.yMax) < 1)
        {
            return;
        }
        playerUiContainer.style.top = placeholderWorldBound.yMin;
        playerUiContainer.style.left = placeholderWorldBound.xMin;
        playerUiContainer.style.width = placeholderWorldBound.width;
        playerUiContainer.style.height = placeholderWorldBound.height;
    }

    private List<PlayerControl> GetPlayerControlsOfVoice(EVoiceId voiceId)
    {
        Dictionary<Voice, List<PlayerControl>> voiceToPlayerControlsMap = new();
        PlayerControls.ForEach(it => voiceToPlayerControlsMap.AddInsideList(it.Voice, it));
        if (voiceToPlayerControlsMap.IsNullOrEmpty())
        {
            return new List<PlayerControl>();
        }

        if (voiceToPlayerControlsMap.Keys.Count >= 2)
        {
            // There are two different sets of lyrics that need to be displayed
            List<Voice> voices = voiceToPlayerControlsMap.Keys
                .OrderBy(voice => voice.Id)
                .ToList();
            Voice firstVoice = voices.FirstOrDefault();
            Voice secondVoice = voices.LastOrDefault();
            List<PlayerControl> playerControlsUsingFirstVoice = voiceToPlayerControlsMap[firstVoice];
            List<PlayerControl> playerControlsUsingSecondVoice = voiceToPlayerControlsMap[secondVoice];

            return voiceId is EVoiceId.P1
                ? playerControlsUsingFirstVoice
                : playerControlsUsingSecondVoice;
        }

        if (voiceToPlayerControlsMap.Keys.Count == 1
            && voiceId is EVoiceId.P1)
        {
            return voiceToPlayerControlsMap.Values.FirstOrDefault();
        }

        return new List<PlayerControl>();
    }

    /**
     * Associates LyricsDisplayer with one of the (duet) players.
     */
    private void InitSingingLyricsControls()
    {
        if (PlayerControls.IsNullOrEmpty()
            || settings.StaticLyricsDisplayMode is EStaticLyricsDisplayMode.None)
        {
            uiDocument.rootVisualElement.Query<VisualElement>(null, R.UssClasses.singingLyricsSentenceUi)
                .ForEach(singingLyricsSentenceUi => singingLyricsSentenceUi.HideByDisplay());
            return;
        }

        singingLyricsControls.Clear();

        VisualElement primaryLyricsContainer = settings.StaticLyricsDisplayMode is EStaticLyricsDisplayMode.Bottom
            ? bottomLyricsContainer
            : topLyricsContainer;
        VisualElement secondaryLyricsContainer = primaryLyricsContainer == topLyricsContainer
            ? bottomLyricsContainer
            : topLyricsContainer;

        List<PlayerControl> playerControlsUsingFirstVoice = GetPlayerControlsOfVoice(EVoiceId.P1);
        List<PlayerControl> playerControlsUsingSecondVoice = GetPlayerControlsOfVoice(EVoiceId.P2);
        if (!playerControlsUsingFirstVoice.IsNullOrEmpty()
            && !playerControlsUsingSecondVoice.IsNullOrEmpty())
        {
            // There are two different sets of lyrics that need to be displayed
            VisualElement firstVoiceLyricsContainer = settings.StaticLyricsDisplayMode is EStaticLyricsDisplayMode.Bottom
                ? secondaryLyricsContainer
                : primaryLyricsContainer;
            VisualElement secondVoiceLyricsContainer = firstVoiceLyricsContainer == primaryLyricsContainer
                ? secondaryLyricsContainer
                : primaryLyricsContainer;

            singingLyricsControls.Add(CreateSingingLyricsControl(firstVoiceLyricsContainer, playerControlsUsingFirstVoice.FirstOrDefault()));
            singingLyricsControls.Add(CreateSingingLyricsControl(secondVoiceLyricsContainer, playerControlsUsingSecondVoice.FirstOrDefault()));
        }
        else
        {
            if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 8)
            {
                // Do not show lyrics, but show player info UI.
                secondaryLyricsContainer.Q<VisualElement>(R.UxmlNames.currentSentenceContainer).HideByDisplay();
                secondaryLyricsContainer.Q<VisualElement>(R.UxmlNames.nextSentenceContainer).HideByDisplay();
            }
            else
            {
                secondaryLyricsContainer.HideByDisplay();
            }

            singingLyricsControls.Add(CreateSingingLyricsControl(primaryLyricsContainer, PlayerControls.FirstOrDefault()));
        }
    }

    private SingingLyricsControl CreateSingingLyricsControl(VisualElement visualElement, PlayerControl playerController)
    {
        Injector lyricsControlInjector = UniInjectUtils.CreateInjector(injector);
        lyricsControlInjector.AddBindingForInstance(playerController);
        SingingLyricsControl singingLyricsControl = lyricsControlInjector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<SingingLyricsControl>();
        return singingLyricsControl;
    }

    private void UpdateLeadingPlayerIcon()
    {
        // Find best player with score > 0
        PlayerControl leadingPlayerControl = null;
        foreach (PlayerControl playerController in PlayerControls)
        {
            if ((leadingPlayerControl == null && playerController.PlayerScoreControl.TotalScore > 0)
               || (leadingPlayerControl != null && playerController.PlayerScoreControl.TotalScore > leadingPlayerControl.PlayerScoreControl.TotalScore))
            {
                leadingPlayerControl = playerController;
            }
        }

        // // Show icon for best player only
        if (leadingPlayerControl != null
            && lastLeadingPlayerControl != leadingPlayerControl)
        {
            leadingPlayerControl.PlayerUiControl.ShowLeadingPlayerIcon();
        }
        foreach (PlayerControl playerController in PlayerControls)
        {
            if (playerController != leadingPlayerControl)
            {
                playerController.PlayerUiControl.HideLeadingPlayerIcon();
            }
        }

        lastLeadingPlayerControl = leadingPlayerControl;
    }

    private void StartVideoOrShowBackgroundImage()
    {
        try
        {
            string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(SongMeta, WebViewUtils.CanHandleWebViewUrl);
            if (SongMetaUtils.ResourceExists(SongMeta, videoUri))
            {
                songVideoPlayer.LoadAndPlayVideoOrShowBackgroundImage(SongMeta);
            }
            else
            {
                songVideoPlayer.ShowBackgroundImage(SongMeta);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to start background video or show image: {ex.Message}");
        }
    }

    void OnDisable()
    {
        if (sceneData.IsRestart)
        {
            sceneData.IsRestart = false;
            sceneData.PositionInMillis = 0;
        }
        else
        {
            sceneData.PositionInMillis = PositionInMillis;
        }
    }

    void Update()
    {
        PlayerControls.ForEach(playerControl =>
        {
            if (!IsPaused)
            {
                playerControl.SetCurrentBeat(CurrentBeat);
                playerControl.UpdateUi();
            }
        });
        timeBarControl?.UpdatePositionIndicator(songAudioPlayer.PositionInMillis, songAudioPlayer.DurationInMillis);
        governanceOverlayTimeBarControl?.UpdatePositionIndicator(songAudioPlayer.PositionInMillis, songAudioPlayer.DurationInMillis);
        singingLyricsControls.ForEach(singingLyricsControl => singingLyricsControl.Update(songAudioPlayer.PositionInMillis));

        UpdateSongStartedStats();

        singSceneGovernanceControl.Update();

        if (sceneData.IsMedley)
        {
            medleyControl.Update();
        }

        if (!IsPaused)
        {
            countdownControl.Update(Time.deltaTime);
        }

        if (settings.UseWebcamAsBackgroundInSingScene)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.useWebcamInSingScene));
        }

        passTheMicControl.Update();

        UpdatePlayerUiContainerHeight();
    }

    public void SkipToNextSingableNoteOrEndOfSong()
    {
        if (sceneData.IsMedley)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_notAvailableDuringMedley));
            return;
        }

        List<int> nextSingableNotes = PlayerControls
            .Select(it => it.GetNextSingableNote(CurrentBeat))
            .Where(nextSingableNote => nextSingableNote != null)
            .Select(nextSingableNote => nextSingableNote.StartBeat)
            .ToList();

        if (nextSingableNotes.IsNullOrEmpty())
        {
            // Skip to end of the audio if last note has been finished.
            int maxBeatInVocals = PlayerControls
                .Select(playerControl => playerControl.MaxBeatInVoice)
                .Max();
            if (CurrentBeat >= maxBeatInVocals)
            {
                SkipToEndOfSong();
            }
            return;
        }
        int nextStartBeat = nextSingableNotes.Min();

        // For debugging, go fast to next lyrics. In production, give the player some time to prepare.
        double offsetInMillis = settings.SkipToNextLyricsTimeInSeconds * 1000;
        double targetPositionInMillis = SongMetaBpmUtils.BeatsToMillis(SongMeta, nextStartBeat) - offsetInMillis;
        if (targetPositionInMillis > 0 && targetPositionInMillis > PositionInMillis)
        {
            SkipToPosition(targetPositionInMillis);
        }
    }

    private void SkipToEndOfSong()
    {
        double targetPosition = songAudioPlayer.DurationInMillis - 2000;
        targetPosition = NumberUtils.Limit(targetPosition, 0, songAudioPlayer.DurationInMillis);
        SkipToPosition(targetPosition);
    }

    public void SkipToPosition(double positionInMillis)
    {
        if (Math.Abs(positionInMillis - songAudioPlayer.PositionInMillis) < 1)
        {
            return;
        }

        if (CancelableEvent.IsCanceledByEvent(beforeSkipEventStream))
        {
            return;
        }

        songAudioPlayer.PositionInMillis = positionInMillis;
        int positionInBeats = (int)SongMetaBpmUtils.MillisToBeats(SongMeta, positionInMillis);
        foreach (PlayerControl playerController in PlayerControls)
        {
            playerController.SkipToBeat(positionInBeats);
        }
        Debug.Log($"Skipped forward to {positionInMillis} milliseconds ({positionInBeats} beats)");
    }

    public void Restart()
    {
        if (CancelableEvent.IsCanceledByEvent(beforeRestartEventStream))
        {
            return;
        }

        sceneData.IsRestart = true;
        sceneNavigator.LoadScene(EScene.SingScene, sceneData);

        restartedEventStream.OnNext(VoidEvent.instance);
    }

    public void OpenSongInEditor()
    {
        if (HasPartyModeSceneData)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.partyMode_error_notAvailable));
            return;
        }
        if (sceneData.IsMedley)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_error_notAvailableDuringMedley));
            return;
        }

        sceneData.PlayerProfileToScoreDataMap = new();
        foreach (PlayerControl playerController in PlayerControls)
        {
            sceneData.PlayerProfileToScoreDataMap.Add(playerController.PlayerProfile, new List<ISingingResultsPlayerScore>
            {
                playerController.PlayerScoreControl.PlayerScore
            });
        }

        SongEditorSceneData songEditorSceneData = new()
        {
            PreviousSceneData = sceneData,
            PreviousScene = EScene.SingScene,
            PositionInMillis = PositionInMillis,
            SongMeta = SongMeta,
            PlayerProfileToMicProfileMap = sceneData.SingScenePlayerData.PlayerProfileToMicProfileMap,
            SelectedPlayerProfiles = sceneData.SingScenePlayerData.SelectedPlayerProfiles,
        };
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToCompanionClient());
        sceneNavigator.LoadScene(EScene.SongEditorScene, songEditorSceneData);
    }

    public void FinishScene(
        bool isAfterEndOfSong,
        bool continueWithNextMedleySong)
    {
        if (hasFinishedScene)
        {
            return;
        }
        hasFinishedScene = true;

        if (continueWithNextMedleySong
            && sceneData.MedleySongIndex >= 0
            && sceneData.MedleySongIndex < sceneData.SongMetas.Count - 1)
        {
            StartNextMedleySong();
            return;
        }

        if (isAfterEndOfSong)
        {
            TriggerAchievementsAfterEndOfSong();
        }

        if (settings.ScoreMode == EScoreMode.None
            && !HasPartyModeSceneData)
        {
            FinishSceneToSongSelect();
        }
        else
        {
            FinishSceneToSingingResults(isAfterEndOfSong);
        }
    }

    private void TriggerAchievementsAfterEndOfSong()
    {
        achievementEventStream.OnNext(new AchievementEvent(AchievementId.completeSong));

        if (settings.VocalsAudioVolumePercent <= 0)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.completeSongWithVocalsVolumeZero));
        }

        completedSongCountSinceAppStart++;
        if (completedSongCountSinceAppStart > 10)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.completeMoreThan10SongsInARow));
        }
    }

    private void StartNextMedleySong()
    {
        // Create scene data to sing next medley song
        SingSceneData newSingSceneData = new(sceneData);
        // Add player scores of this song
        foreach (PlayerControl playerControl in PlayerControls)
        {
            if (!newSingSceneData.PlayerProfileToScoreDataMap.ContainsKey(playerControl.PlayerProfile))
            {
                newSingSceneData.PlayerProfileToScoreDataMap.Add(playerControl.PlayerProfile, new List<ISingingResultsPlayerScore>());
            }
            newSingSceneData.PlayerProfileToScoreDataMap[playerControl.PlayerProfile].Add(playerControl.PlayerScoreControl.PlayerScore);
        }
        // Continue with next medley song
        newSingSceneData.MedleySongIndex++;
        sceneNavigator.LoadScene(EScene.SingScene, newSingSceneData, true);
    }

    private void FinishSceneToSongSelect()
    {
        // Open song select without recording scores
        SongSelectSceneData songSelectSceneData = new();
        songSelectSceneData.SongMeta = SongMeta;
        songSelectSceneData.partyModeSceneData = sceneData.partyModeSceneData;
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToCompanionClient());
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    private void FinishSceneToSingingResults(bool isAfterEndOfSong)
    {
        // Open the singing results scene.
        SingingResultsSceneData singingResultsSceneData = new();
        singingResultsSceneData.SongMetas = sceneData.SongMetas;
        singingResultsSceneData.IsMedley = sceneData.IsMedley;
        singingResultsSceneData.SongDurationInMillis = (int)songAudioPlayer.DurationInMillis;
        singingResultsSceneData.partyModeSceneData = sceneData.partyModeSceneData;
        singingResultsSceneData.GameRoundSettings = sceneData.gameRoundSettings;

        // Add scores, either for individual players, or as one common score.
        List<HighScoreEntry> highScoreEntries = new();
        if (IsIndividualScore)
        {
            // Add and record score for each player individually.
            singingResultsSceneData.PlayerProfileToMicProfileMap = sceneData.SingScenePlayerData.PlayerProfileToMicProfileMap;
            PlayerControls.ForEach(playerControl =>
            {
                ISingingResultsPlayerScore singingResultsPlayerScore = GetSingingResultsPlayerScore(playerControl);
                singingResultsSceneData.AddPlayerScores(playerControl.PlayerProfile, singingResultsPlayerScore);
            });

            highScoreEntries = PlayerControls
                .Select(playerControl => new HighScoreEntry(playerControl.PlayerProfile.Name,
                    playerControl.PlayerProfile.Difficulty,
                    playerControl.PlayerScoreControl.TotalScore,
                    EScoreMode.Individual))
                .ToList();
        }
        else if (IsCommonScore)
        {
            // Add and record score as average of all players.
            List<ISingingResultsPlayerScore> scoreControlDatas = PlayerControls
                .Select(playerControl => GetSingingResultsPlayerScore(playerControl))
                .ToList();
            string commonPlayerProfileName = PlayerControls
                .Select(playerControl => playerControl.PlayerProfile.Name)
                .JoinWith(settings.CommonScoreNameSeparator);
            EDifficulty easiestPlayerProfileDifficulty = PlayerControls
                .FindMinElement(playerControl => (int)playerControl.PlayerProfile.Difficulty)
                .PlayerProfile.Difficulty;
            string commonProfileImagePath = playerProfileImageManager.GetFinalPlayerProfileImagePath(PlayerControls.Select(it => it.PlayerProfile).FirstOrDefault());
            PlayerProfile commonPlayerProfile = new(commonPlayerProfileName, easiestPlayerProfileDifficulty, commonProfileImagePath);
            ISingingResultsPlayerScore commonScore = CreateAveragePlayerScoreControlData(scoreControlDatas);
            singingResultsSceneData.AddPlayerScores(commonPlayerProfile, commonScore);

            // Define common mic profile
            MicProfile commonMicProfile = PlayerControls
                    .Select(it => it.MicProfile)
                    .FirstOrDefault(it => it != null);
            singingResultsSceneData.PlayerProfileToMicProfileMap = new()
            {
                { commonPlayerProfile, commonMicProfile }
            };

            HighScoreEntry commonHighScoreEntry = new HighScoreEntry(
                commonPlayerProfileName,
                easiestPlayerProfileDifficulty,
                commonScore.TotalScore,
                EScoreMode.CommonAverage);
            highScoreEntries = new() { commonHighScoreEntry };
        }

        // Check if the full song has been sung, i.e., the playback position is after the last note.
        // This determines whether the statistics should be updated and the score should be recorded.
        bool isAfterLastNote = true;
        PlayerControls.ForEach(playerControl =>
        {
            Note lastNoteInSong = playerControl.GetLastNoteInSong();
            if (lastNoteInSong != null
                && !isAfterEndOfSong
                && CurrentBeat < lastNoteInSong.EndBeat)
            {
                isAfterLastNote = false;
            }
        });
        if (isAfterLastNote)
        {
            UpdateSongFinishedStatistics();
            UpdateHighScoreStatistics(highScoreEntries);
        }

        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToCompanionClient());
        sceneNavigator.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private ISingingResultsPlayerScore GetSingingResultsPlayerScore(PlayerControl playerControl)
    {
        if (sceneData.IsMedley)
        {
            // Use the average of all medley songs
            if (sceneData.PlayerProfileToScoreDataMap.TryGetValue(playerControl.PlayerProfile, out List<ISingingResultsPlayerScore> scoreDatas)
                && scoreDatas.Count > 0)
            {
                List<ISingingResultsPlayerScore> allScoreDatas = new(scoreDatas);
                // Include the score for the current song
                allScoreDatas.Add(playerControl.PlayerScoreControl.PlayerScore);
                return CreateAveragePlayerScoreControlData(allScoreDatas);
            }
        }

        // Use the current score data of the player
        return new SingingResultsPlayerScore(playerControl.PlayerScoreControl.PlayerScore);
    }

    private ISingingResultsPlayerScore CreateAveragePlayerScoreControlData<T>(List<T> scoreDatas)
        where T : ISingingResultsPlayerScore
    {
        SingingResultsPlayerScore averageScore = new()
        {
            NormalNotesTotalScore = (int)scoreDatas.Select(scoreControlData => scoreControlData.NormalNotesTotalScore).Average(),
            GoldenNotesTotalScore = (int)scoreDatas.Select(scoreData => scoreData.GoldenNotesTotalScore).Average(),
            PerfectSentenceBonusTotalScore = (int)scoreDatas.Select(scoreControlData => scoreControlData.PerfectSentenceBonusTotalScore).Average(),
            ModTotalScore = (int)scoreDatas.Select(scoreControlData => scoreControlData.ModTotalScore).Average(),
        };
        return averageScore;
    }

    private List<CompanionClientHandlerAndMicProfile> GetCompanionClientHandlers()
    {
        IEnumerable<MicProfile> micProfiles = PlayerControls.Select(playerProfile => playerProfile.MicProfile);
        return serverSideCompanionClientManager.GetCompanionClientHandlers(micProfiles);
    }

    private void UpdateSongStartedStats()
    {
        if (hasRecordedSongStartedStatistics)
        {
            return;
        }

        // Save information that the song has been started after some seconds or half of the song.
        float songSingingDuration = Time.time - startTimeInSeconds;
        float songDurationInSeconds = (float)songAudioPlayer.DurationInMillis / 1000;
        if (songSingingDuration >= 30
            || (songDurationInSeconds > 0
                && songSingingDuration >= songDurationInSeconds / 2))
        {
            hasRecordedSongStartedStatistics = true;
            StatisticsUtils.RecordSongStarted(statistics, SongMeta);
        }
    }

    private void UpdateSongFinishedStatistics()
    {
        if (hasRecordedSongFinishedStatistics
            || sceneData.IsMedley)
        {
            // Medleys and party mode are not recorded
            return;
        }

        hasRecordedSongFinishedStatistics = true;
        StatisticsUtils.RecordSongFinished(statistics, SongMeta);
    }

    private void UpdateHighScoreStatistics(List<HighScoreEntry> highScoreEntries)
    {
        if (hasRecordedHighScoreStatistics
            || sceneData.IsMedley
            || sceneData.gameRoundSettings.AnyModifierActive
            || highScoreEntries.IsNullOrEmpty())
        {
            // Medleys and game modifiers do not record any high score
            return;
        }

        hasRecordedHighScoreStatistics = true;
        StatisticsUtils.RecordSongHighScore(statistics, SongMeta, highScoreEntries);
    }

    private PlayerControl CreatePlayerControl(PlayerProfile playerProfile, MicProfile micProfile, int playerIndex)
    {
        Voice voice = GetVoice(playerProfile);

        PlayerControl playerControl = Instantiate<PlayerControl>(playerControlPrefab);

        Injector playerControlInjector = UniInjectUtils.CreateInjector(injector);
        playerControlInjector.AddBindingForInstance(playerProfile);
        playerControlInjector.AddBindingForInstance(voice);
        playerControlInjector.AddBindingForInstance(micProfile);
        playerControlInjector.AddBindingForInstance(playerControlInjector, RebindingBehavior.Ignore);
        playerControlInjector.AddBinding(new UniInjectBinding("playerProfileIndex", new ExistingInstanceProvider<int>(playerIndex)));
        playerControlInjector.Inject(playerControl);

        PlayerControls.Add(playerControl);

        playerControl.PlayerMicPitchTracker.InitPitchDetection();

        return playerControl;
    }

    private void AddPlayerUisToUiDocument()
    {
        // Add the players first that are singing the first voice.
        // This corresponds with the positioning of the player profile UI and lyrics boxes.
        List<PlayerControl> playerControlsUsingFirstVoice = GetPlayerControlsOfVoice(EVoiceId.P1);
        List<PlayerControl> playerControlsUsingSecondVoice = GetPlayerControlsOfVoice(EVoiceId.P2);

        foreach (PlayerControl playerControl in playerControlsUsingFirstVoice)
        {
            AddPlayerUi(playerControl.PlayerUiControl.RootVisualElement, PlayerControls.IndexOf(playerControl));
        }

        foreach (PlayerControl playerControl in playerControlsUsingSecondVoice)
        {
            AddPlayerUi(playerControl.PlayerUiControl.RootVisualElement, PlayerControls.IndexOf(playerControl));
        }
    }

    private void AddPlayerUi(VisualElement visualElement, int playerIndex)
    {
        int playerCount = sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count;
        if (playerCount <= 3)
        {
            playerUiContainer.Add(visualElement);
            return;
        }

        int columnIndex = (int)((float)playerUiColumns.Length * (float)playerIndex / (float)playerCount);
        VisualElement column = playerUiColumns[columnIndex];
        column.Add(visualElement);
    }

    private EExtendedVoiceId GetExtendedVoiceId(PlayerProfile playerProfile)
    {
        Dictionary<EVoiceId, string> voiceIdToDisplayName = SongMetaUtils.GetVoiceIdToDisplayName(SongMeta);
        List<EVoiceId> voiceIds = voiceIdToDisplayName.Keys.ToList();
        if (voiceIds.Count <= 1)
        {
            return EExtendedVoiceId.P1;
        }

        if (sceneData.SingScenePlayerData.PlayerProfileToVoiceIdMap.TryGetValue(playerProfile, out EExtendedVoiceId voiceId))
        {
            return voiceId;
        }

        if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count == 1)
        {
            return EExtendedVoiceId.P1;
        }

        int voiceIndex = sceneData.SingScenePlayerData.SelectedPlayerProfiles.IndexOf(playerProfile) % voiceIds.Count;
        List<EExtendedVoiceId> extendedVoiceIds = EnumUtils.GetValuesAsList<EExtendedVoiceId>();
        return extendedVoiceIds[voiceIndex];
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        songAudioPlayer.PauseAudio();
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.StopRecording());

        // Trigger achievement
        if (songAudioPlayer.PositionInMillis > 60000)
        {
            achievementEventStream.OnNext(new AchievementEvent(AchievementId.pauseSingingAfterOneMinute));
        }

        pausedEventStream.OnNext(VoidEvent.instance);
    }

    public void Unpause()
    {
        if (!IsPaused)
        {
            return;
        }

        songAudioPlayer.PlayAudio();
        PlayerControls.ForEach(playerControl =>
        {
            playerControl.PlayerMicPitchTracker.StartRecording();
            playerControl.PlayerMicPitchTracker.SendPositionToClientRapidly();
        });

        unpausedEventStream.OnNext(VoidEvent.instance);
    }

    public void AbortSceneToSongSelect()
    {
        sceneNavigator.LoadScene(EScene.SongSelectScene, new SongSelectSceneData()
        {
            SongMeta = SongMeta,
            partyModeSceneData = PartyModeSceneData,
        });
    }

    public void TogglePlayPause()
    {
        if (IsPaused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    private async Awaitable StartAudioPlaybackAsync()
    {
        if (songAudioPlayer.IsPlaying)
        {
            Debug.LogWarning("Song already playing");
            return;
        }

        double startPositionInMillis = GetStartPositionInMillis();
        bool streamAudio = InaccurateMp3WorkaroundUtils.ShouldStreamAudio(SongMetaUtils.GetAudioUri(SongMeta));

        try
        {
            await songAudioPlayer.LoadAndPlayAsync(SongMeta, startPositionInMillis, streamAudio);
            timeBarControl?.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationInMillis);
            governanceOverlayTimeBarControl?.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationInMillis);

            if (sceneData.StartPaused)
            {
                songAudioPlayer.PauseAudio();
            }
            else
            {
                songAudioPlayer.PlayAudio();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load audio: {ex.Message}");

            if (ex is not DestroyedAlreadyException)
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason", ex.Message));
            }
            PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToCompanionClient());
            sceneNavigator.LoadScene(EScene.SongSelectScene);
        }
    }

    private double GetStartPositionInMillis()
    {
        if (sceneData.PositionInMillis > 0)
        {
            return sceneData.PositionInMillis;
        }

        if (SongMeta.StartInMillis > 0)
        {
            return SongMeta.StartInMillis;
        }

        return 0;
    }

    public List<IBinding> GetBindings()
    {
        // Binding happens before the injection finished. Thus, no fields can be used here that have been injected.
        sceneData = SceneNavigator.GetSceneDataOrThrow<SingSceneData>();

        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(sceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(webcamControl);
        bb.BindExistingInstance(singSceneGovernanceControl);
        bb.BindExistingInstance(singSceneFinisher);
        bb.BindExistingInstance(countdownControl);
        bb.BindExistingInstance(medleyControl);
        bb.BindExistingInstance(audioFadeInControl);
        bb.BindExistingInstance(alternativeAudioPlayer);
        bb.Bind(nameof(playerUi)).ToExistingInstance(playerUi);
        bb.Bind(nameof(playerInfoUi)).ToExistingInstance(playerInfoUi);
        bb.Bind(nameof(sentenceRatingUi)).ToExistingInstance(sentenceRatingUi);
        bb.Bind(nameof(noteUi)).ToExistingInstance(noteUi);
        bb.Bind(nameof(perfectEffectStarUi)).ToExistingInstance(perfectEffectStarUi);
        bb.Bind(nameof(goldenNoteStarUi)).ToExistingInstance(goldenNoteStarUi);
        bb.Bind(nameof(goldenNoteHitStarUi)).ToExistingInstance(goldenNoteHitStarUi);
        return bb.GetBindings();
    }

    private Voice GetVoice(PlayerProfile playerProfile)
    {
        EExtendedVoiceId voiceId = GetExtendedVoiceId(playerProfile);
        Voice voice = GetVoiceByExtendedVoiceId(voiceId);
        if (voice == null)
        {
            Voice fallbackVoice = SongMeta.Voices.FirstOrDefault();
            string voiceIdCsv = SongMeta.Voices.Select(it => it.Id).JoinWith(", ");
            Debug.LogError($"The song data does not contain a voice with id {voiceId}."
                           + $" Available voice ids: {voiceIdCsv}. Using voice {fallbackVoice?.Id} instead.");
            return fallbackVoice;
        }
        return voice;
    }

    private Voice GetVoiceByExtendedVoiceId(EExtendedVoiceId extendedVoiceId)
    {
        if (extendedVoiceId is EExtendedVoiceId.Merged)
        {
            return VoicesMerger.Merge(SongMeta.Voices.ToList());
        }

        if (extendedVoiceId.TryGetVoiceId(out EVoiceId voiceId))
        {
            return SongMetaUtils.GetVoiceById(SongMeta, voiceId);
        }

        Debug.LogWarning($"Failed to find voice for extended voice id: {extendedVoiceId}. Using first voice instead.");
        return SongMeta.Voices.FirstOrDefault();
    }

    private void UpdateInputLegend()
    {
        // inputLegend.Query<Label>()
        //     .Where(label => label is not FontIcon)
        //     .ForEach(label => label.RemoveFromHierarchy());
        //
        // InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
        //     Translation.Get(R.Messages.back),
        //     inputLegend);
        // InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_openSongEditor,
        //     Translation.Get(R.Messages.action_openSongEditor),
        //     inputLegend);
        // InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_restartSong,
        //     Translation.Get(R.Messages.action_restart),
        //     inputLegend);
        //
        // if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        // {
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         Translation.Get(R.Messages.continue_),
        //         Translation.Get(R.Messages.action_doubleTap))));
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         Translation.Get(R.Messages.action_openContextMenu),
        //         Translation.Get(R.Messages.action_longPress))));
        // }
        // else
        // {
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         Translation.Get(R.Messages.action_skipToNextLyrics),
        //         Translation.Get(R.Messages.action_navigateRight))));
        // }
    }

    public void FadeOutLyrics(Voice voice, float animTimeInSeconds)
    {
        foreach (SingingLyricsControl singingLyricsControl in singingLyricsControls)
        {
            if (singingLyricsControl.Voice == voice)
            {
                singingLyricsControl.FadeOut(animTimeInSeconds);
            }
        }
    }

    public void FadeInLyrics(Voice voice, float animTimeInSeconds)
    {
        foreach (SingingLyricsControl singingLyricsControl in singingLyricsControls)
        {
            if (singingLyricsControl.Voice == voice)
            {
                singingLyricsControl.FadeIn(animTimeInSeconds);
            }
        }
    }
}
