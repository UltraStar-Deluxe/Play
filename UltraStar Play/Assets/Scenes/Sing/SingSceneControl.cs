using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using ProTrans;
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
    private UiManager uiManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private Statistics statistics;

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
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

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

    public double DurationOfSongInMillis => songAudioPlayer.DurationOfSongInMillis;
    public double PositionInSongInMillis => songAudioPlayer.PositionInSongInMillis;
    public double CurrentBeat => songAudioPlayer.GetCurrentBeat(false);

    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSettings PartyModeSettings => sceneData.partyModeSceneData.PartyModeSettings;
    public bool IsPassTheMic => HasPartyModeSceneData &&
                                sceneData.gameRoundSettings.modifiers.AnyMatch(modifier => modifier is PassTheMicGameRoundModifier);

    private SingingLyricsControl topSingingLyricsControl;
    private SingingLyricsControl bottomSingingLyricsControl;

    private readonly TimeBarControl timeBarControl = new();
    private readonly TimeBarControl governanceOverlayTimeBarControl = new();

    private MessageDialogControl dialogControl;

    private readonly SingSceneGovernanceControl singSceneGovernanceControl = new();
    private readonly CommonScoreControl commonScoreControl = new();
    private readonly SingSceneCountdownControl countdownControl = new();
    private readonly SingSceneAudioFadeInControl audioFadeInControl = new();
    private readonly SingSceneMedleyControl medleyControl = new();
    private readonly SingScenePassTheMicControl passTheMicControl = new();

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
        string playerProfilesCsv = sceneData.SingScenePlayerData.SelectedPlayerProfiles.Select(it => it.Name).ToCsv();
        Debug.Log($"{playerProfilesCsv} start (or continue) singing of {SongMeta.Title} at {sceneData.PositionInSongInMillis} ms.");

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
                    || lobbyMemberPlayerProfile.UnityNetcodeClientId == onlineMultiplayerManager.OwnLobbyMemberUnityNetcodeClientId))
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

            if (sceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out List<PlayerScoreControlData> scoreDatas))
            {
                if (sceneData.MedleySongIndex < 0)
                {
                    // No medley, select first score data
                    playerControl.PlayerScoreControl.ScoreData = scoreDatas.FirstOrDefault();
                }
                else if (sceneData.MedleySongIndex < scoreDatas.Count)
                {
                    // This is a medley (or short song), select score data for this medley entry song
                    playerControl.PlayerScoreControl.ScoreData = scoreDatas[sceneData.MedleySongIndex];
                }

                if (playerControl.PlayerScoreControl.ScoreData != null)
                {
                    playerControl.PlayerUiControl.ShowTotalScore(playerControl.PlayerScoreControl.ScoreData.TotalScore, false);
                }
            }

            // Update leading player icon
            if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 1)
            {
                playerControl.PlayerScoreControl.SentenceScoreEventStream
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

        // Associate LyricsDisplayer with one of the (duet) players
        InitSingingLyricsControls();

        StartAudioPlayback();

        StartVideoOrShowBackgroundImage();

        // Input legend (in pause overlay)
        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        // Progress bar to show time in song
        songTimeProgressBar.value = 0;
        songAudioPlayer.PositionInSongEventStream.Subscribe(_ =>
        {
            double startTagInMillis = SongMeta.StartInMillis;
            double endTagInMillis = SongMeta.EndInMillis;
            double positionInSongInMillisConsideringStartTag = songAudioPlayer.PositionInSongInMillis - startTagInMillis;
            double durationOfSongInMillisConsideringStartAndEndTag = songAudioPlayer.DurationOfSongInMillis - startTagInMillis - endTagInMillis;
            double progressInPercent = 100 * (positionInSongInMillisConsideringStartTag / durationOfSongInMillisConsideringStartAndEndTag);
            songTimeProgressBar.value = (float) progressInPercent;
        });
        settings.ObserveEveryValueChanged(it => it.ShowSongProgressBar)
            .Subscribe(newValue =>
            {
                songTimeProgressBar.SetVisibleByDisplay(newValue is ESongProgressBar.Plain);
                detailedTimeBar.SetVisibleByDisplay(newValue is ESongProgressBar.Detailed);
            });

        // Update TimeBar every second
        StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(1f, () =>
        {
            timeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
            governanceOverlayTimeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        }));

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
            achievementEventStream.OnNext(AchievementId.startDuetWithDifferentLyrics);
        }

        if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 4)
        {
            achievementEventStream.OnNext(AchievementId.startSongWithFourOrMorePlayers);
        }

        // Medley with at least two entries
        if (sceneData.IsMedley
            && sceneData.MedleySongIndex >= 0
            && sceneData.SongMetas.Count > 1)
        {
            achievementEventStream.OnNext(AchievementId.startMedleyWithAtLeastTwoSongs);
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
        string title = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_title);
        string message = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_message,
            "playerNameCsv", playerNameCsv);

        dialogControl = UiManager.Instance.CreateDialogControl(title);
        dialogControl.DialogClosedEventStream.Subscribe(_ => dialogControl = null);
        dialogControl.Message = message;

        ThemeManager.ApplyThemeSpecificStylesToVisualElements(dialogControl.DialogRootVisualElement);
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

    private List<PlayerControl> GetPlayerControlsOfVoice(bool isFirstVoice)
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

            if (isFirstVoice)
            {
                return playerControlsUsingFirstVoice;
            }
            else
            {
                return playerControlsUsingSecondVoice;
            }
        }
        else if (voiceToPlayerControlsMap.Keys.Count == 1
                 && isFirstVoice)
        {
            return voiceToPlayerControlsMap.Values.FirstOrDefault();
        }

        return new List<PlayerControl>();
    }

    private void InitSingingLyricsControls()
    {
        if (PlayerControls.IsNullOrEmpty()
            || !settings.ShowStaticLyrics)
        {
            uiDocument.rootVisualElement.Query<VisualElement>(null, R.UssClasses.singingLyricsSentenceUi)
                .ForEach(singingLyricsSentenceUi => singingLyricsSentenceUi.HideByDisplay());
            return;
        }

        SingingLyricsControl CreateSingingLyricsControl(VisualElement visualElement, PlayerControl playerController)
        {
            Injector lyricsControlInjector = UniInjectUtils.CreateInjector(injector);
            lyricsControlInjector.AddBindingForInstance(playerController);
            SingingLyricsControl singingLyricsControl = lyricsControlInjector
                .WithRootVisualElement(visualElement)
                .CreateAndInject<SingingLyricsControl>();
            return singingLyricsControl;
        }

        List<PlayerControl> playerControlsUsingFirstVoice = GetPlayerControlsOfVoice(true);
        List<PlayerControl> playerControlsUsingSecondVoice = GetPlayerControlsOfVoice(false);
        if (!playerControlsUsingFirstVoice.IsNullOrEmpty()
            && !playerControlsUsingSecondVoice.IsNullOrEmpty())
        {
            // There are two different sets of lyrics that need to be displayed
            topSingingLyricsControl = CreateSingingLyricsControl(topLyricsContainer, playerControlsUsingFirstVoice.FirstOrDefault());
            bottomSingingLyricsControl = CreateSingingLyricsControl(bottomLyricsContainer, playerControlsUsingSecondVoice.FirstOrDefault());
        }
        else
        {
            if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 8)
            {
                // Do not show lyrics at the top, but show player info UI.
                topLyricsContainer.Q<VisualElement>(R.UxmlNames.currentSentenceContainer).HideByDisplay();
                topLyricsContainer.Q<VisualElement>(R.UxmlNames.nextSentenceContainer).HideByDisplay();
            }
            else
            {
                topLyricsContainer.HideByDisplay();
            }
            bottomSingingLyricsControl = CreateSingingLyricsControl(bottomLyricsContainer, PlayerControls.FirstOrDefault());
        }
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
                songVideoPlayer.LoadAndPlaySongVideoOrShowBackgroundImage(SongMeta);
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
            sceneData.PositionInSongInMillis = 0;
        }
        else
        {
            sceneData.PositionInSongInMillis = PositionInSongInMillis;
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
        timeBarControl?.UpdatePositionIndicator(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        governanceOverlayTimeBarControl?.UpdatePositionIndicator(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        topSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);
        bottomSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);

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
            achievementEventStream.OnNext(AchievementId.useWebcamInSingScene);
        }

        passTheMicControl.Update();

        UpdatePlayerUiContainerHeight();
    }

    public void SkipToNextSingableNoteOrEndOfSong()
    {
        if (sceneData.IsMedley)
        {
            // Skipping is not allowed in a medley.
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
        double offsetInMillis = Application.isEditor ? 500 : 2000;
        double targetPositionInMillis = SongMetaBpmUtils.BeatsToMillis(SongMeta, nextStartBeat) - offsetInMillis;
        if (targetPositionInMillis > 0 && targetPositionInMillis > PositionInSongInMillis)
        {
            SkipToPositionInSong(targetPositionInMillis);
        }
    }

    private void SkipToEndOfSong()
    {
        double targetPositionInSong = songAudioPlayer.DurationOfSongInMillis - 2000;
        targetPositionInSong = NumberUtils.Limit(targetPositionInSong, 0, songAudioPlayer.DurationOfSongInMillis);
        SkipToPositionInSong(targetPositionInSong);
    }

    public void SkipToPositionInSong(double positionInSongInMillis)
    {
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        int positionInSongInBeats = (int)SongMetaBpmUtils.MillisToBeats(SongMeta, positionInSongInMillis);
        foreach (PlayerControl playerController in PlayerControls)
        {
            playerController.SkipToBeat(positionInSongInBeats);
        }
        Debug.Log($"Skipped forward to {positionInSongInMillis} milliseconds ({positionInSongInBeats} beats)");
    }

    public void Restart()
    {
        sceneData.IsRestart = true;
        sceneNavigator.LoadScene(EScene.SingScene, sceneData);
    }

    public void OpenSongInEditor()
    {
        if (HasPartyModeSceneData)
        {
            UiManager.CreateNotification("Song editor not available in Team & Tournament mode.");
            return;
        }
        if (sceneData.IsMedley)
        {
            UiManager.CreateNotification("Song editor not available during medley.");
            return;
        }

        int maxBeatToScore = PlayerControls
            .Select(playerController => playerController.PlayerScoreControl.NextBeatToScore)
            .Max();

        sceneData.PlayerProfileToScoreDataMap = new();
        foreach (PlayerControl playerController in PlayerControls)
        {
            sceneData.PlayerProfileToScoreDataMap.Add(playerController.PlayerProfile, new List<PlayerScoreControlData> { playerController.PlayerScoreControl.ScoreData });
        }

        SongEditorSceneData songEditorSceneData = new()
        {
            PreviousSceneData = sceneData,
            PreviousScene = EScene.SingScene,
            PositionInSongInMillis = PositionInSongInMillis,
            SongMeta = SongMeta,
            PlayerProfileToMicProfileMap = sceneData.SingScenePlayerData.PlayerProfileToMicProfileMap,
            SelectedPlayerProfiles = sceneData.SingScenePlayerData.SelectedPlayerProfiles,
        };
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
        sceneNavigator.LoadScene(EScene.SongEditorScene, songEditorSceneData);
    }

    public void FinishScene(bool isAfterEndOfSong, bool continueWithNextMedleySong)
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
        achievementEventStream.OnNext(AchievementId.completeSong);

        if (settings.VocalsAudioVolumePercent <= 0)
        {
            achievementEventStream.OnNext(AchievementId.completeSongWithVocalsVolumeZero);
        }

        completedSongCountSinceAppStart++;
        if (completedSongCountSinceAppStart > 10)
        {
            achievementEventStream.OnNext(AchievementId.completeMoreThan10SongsInARow);
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
                newSingSceneData.PlayerProfileToScoreDataMap.Add(playerControl.PlayerProfile, new List<PlayerScoreControlData>());
            }
            newSingSceneData.PlayerProfileToScoreDataMap[playerControl.PlayerProfile].Add(playerControl.PlayerScoreControl.ScoreData);
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
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    private void FinishSceneToSingingResults(bool isAfterEndOfSong)
    {
        // Open the singing results scene.
        SingingResultsSceneData singingResultsSceneData = new();
        singingResultsSceneData.SongMetas = sceneData.SongMetas;
        singingResultsSceneData.IsMedley = sceneData.IsMedley;
        singingResultsSceneData.SongDurationInMillis = (int)songAudioPlayer.DurationOfSongInMillis;
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
                PlayerScoreControlData playerScoreControlData = GetPlayerScoreDataForSingingResultsScene(playerControl);
                singingResultsSceneData.AddPlayerScores(playerControl.PlayerProfile, playerScoreControlData);
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
            List<PlayerScoreControlData> scoreControlDatas = PlayerControls
                .Select(playerControl => GetPlayerScoreDataForSingingResultsScene(playerControl))
                .ToList();
            string commonPlayerProfileName = PlayerControls
                .Select(playerControl => playerControl.PlayerProfile.Name)
                .JoinWith(settings.CommonScoreNameSeparator);
            EDifficulty easiestPlayerProfileDifficulty = PlayerControls
                .FindMinElement(playerControl => (int)playerControl.PlayerProfile.Difficulty)
                .PlayerProfile.Difficulty;
            string commonProfileImagePath = uiManager.GetFinalPlayerProfileImagePath(PlayerControls.Select(it => it.PlayerProfile).FirstOrDefault());
            PlayerProfile commonPlayerProfile = new(commonPlayerProfileName, easiestPlayerProfileDifficulty, commonProfileImagePath);
            PlayerScoreControlData commonScoreData = CreateAveragePlayerScoreControlData(scoreControlDatas);
            singingResultsSceneData.AddPlayerScores(commonPlayerProfile, commonScoreData);

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
                commonScoreData.TotalScore,
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

        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
        sceneNavigator.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private PlayerScoreControlData GetPlayerScoreDataForSingingResultsScene(PlayerControl playerControl)
    {
        if (sceneData.IsMedley)
        {
            // Use the average of all medley songs
            if (sceneData.PlayerProfileToScoreDataMap.TryGetValue(playerControl.PlayerProfile, out List<PlayerScoreControlData> scoreDatas)
                && scoreDatas.Count > 0)
            {
                List<PlayerScoreControlData> allScoreDatas = new(scoreDatas);
                // Include the score for the current song
                allScoreDatas.Add(playerControl.PlayerScoreControl.ScoreData);
                return CreateAveragePlayerScoreControlData(allScoreDatas);
            }
        }

        // Use the current score data of the player
        return playerControl.PlayerScoreControl.ScoreData;
    }

    private PlayerScoreControlData CreateAveragePlayerScoreControlData(List<PlayerScoreControlData> scoreControlDatas)
    {
        PlayerScoreControlData averageScoreData = new()
        {
            TotalScore = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.TotalScore).Average(),
            GoldenNotesTotalScore = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.GoldenNotesTotalScore).Average(),
            NormalNotesTotalScore = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.NormalNotesTotalScore).Average(),
            PerfectSentenceBonusTotalScore = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.PerfectSentenceBonusTotalScore).Average(),
            PerfectSentenceCount = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.PerfectSentenceCount).Average(),
            TotalSentenceCount = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.TotalSentenceCount).Average(),
            NormalNoteLengthTotal = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.NormalNoteLengthTotal).Average(),
            GoldenNoteLengthTotal = (int)scoreControlDatas.Select(scoreControlData => scoreControlData.GoldenNoteLengthTotal).Average()
        };
        return averageScoreData;
    }

    private List<ConnectedClientHandlerAndMicProfile> GetConnectedClientHandlers()
    {
        IEnumerable<MicProfile> micProfiles = PlayerControls.Select(playerProfile => playerProfile.MicProfile);
        return serverSideConnectRequestManager.GetConnectedClientHandlers(micProfiles);
    }

    private void UpdateSongStartedStats()
    {
        if (hasRecordedSongStartedStatistics)
        {
            return;
        }

        // Save information that the song has been started after some seconds or half of the song.
        float songSingingDuration = Time.time - startTimeInSeconds;
        float songDurationInSeconds = (float)songAudioPlayer.DurationOfSongInMillis / 1000;
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
        playerControlInjector.AddBinding(new Binding("playerProfileIndex", new ExistingInstanceProvider<int>(playerIndex)));
        playerControlInjector.Inject(playerControl);

        PlayerControls.Add(playerControl);

        playerControl.PlayerMicPitchTracker.InitPitchDetection();

        return playerControl;
    }

    private void AddPlayerUisToUiDocument()
    {
        // Add the players first that are singing the first voice.
        // This corresponds with the positioning of the player profile UI and lyrics boxes.
        List<PlayerControl> playerControlsUsingFirstVoice = GetPlayerControlsOfVoice(true);
        List<PlayerControl> playerControlsUsingSecondVoice = GetPlayerControlsOfVoice(false);

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
        if (songAudioPlayer.PositionInSongInMillis > 60000)
        {
            achievementEventStream.OnNext(AchievementId.pauseSingingAfterOneMinute);
        }
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
            playerControl.PlayerMicPitchTracker.SendPositionInSongToClientRapidly();
        });
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            Pause();
        }
        else
        {
            Unpause();
        }
    }

    private void StartAudioPlayback()
    {
        if (songAudioPlayer.IsPlaying)
        {
            Debug.LogWarning("Song already playing");
            return;
        }

        double startPositionInSongInMillis = GetStartPositionInSongInMillis();

        songAudioPlayer.LoadAndPlaySongAudioAsObservable(SongMeta, startPositionInSongInMillis)
            .CatchIgnore((Exception ex) =>
            {
                // Loading the audio failed.
                PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
                sceneNavigator.LoadScene(EScene.SongSelectScene);
            })
            .Subscribe(_ =>
            {
                timeBarControl?.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationOfSongInMillis);
                governanceOverlayTimeBarControl?.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationOfSongInMillis);
                songAudioPlayer.PlayAudio();
            });

        SkipToPositionInSong(startPositionInSongInMillis);
    }

    private double GetStartPositionInSongInMillis()
    {
        if (sceneData.PositionInSongInMillis > 0)
        {
            return sceneData.PositionInSongInMillis;
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
            string voiceIdCsv = SongMeta.Voices.Select(it => it.Id).ToCsv();
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
            return SongMetaUtils.CreateMergedVoice(SongMeta.Voices.ToList());
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
        //     TranslationManager.GetTranslation(R.Messages.back),
        //     inputLegend);
        // InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_openSongEditor,
        //     TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
        //     inputLegend);
        // InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_restartSong,
        //     TranslationManager.GetTranslation(R.Messages.action_restart),
        //     inputLegend);
        //
        // if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        // {
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         TranslationManager.GetTranslation(R.Messages.continue_),
        //         TranslationManager.GetTranslation(R.Messages.action_doubleTap))));
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         TranslationManager.GetTranslation(R.Messages.action_openContextMenu),
        //         TranslationManager.GetTranslation(R.Messages.action_longPress))));
        // }
        // else
        // {
        //     inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
        //         TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
        //         TranslationManager.GetTranslation(R.Messages.action_navigateRight))));
        // }
    }

    public void FadeOutLyrics(Voice voice, float animTimeInSeconds)
    {
        if (topSingingLyricsControl != null
            && topSingingLyricsControl.Voice == voice)
        {
            topSingingLyricsControl.FadeOut(animTimeInSeconds);
        }

        if (bottomSingingLyricsControl != null
            && bottomSingingLyricsControl.Voice == voice)
        {
            bottomSingingLyricsControl.FadeOut(animTimeInSeconds);
        }
    }

    public void FadeInLyrics(Voice voice, float animTimeInSeconds)
    {
        if (topSingingLyricsControl != null
            && topSingingLyricsControl.Voice == voice)
        {
            topSingingLyricsControl.FadeIn(animTimeInSeconds);
        }

        if (bottomSingingLyricsControl != null
            && bottomSingingLyricsControl.Voice == voice)
        {
            bottomSingingLyricsControl.FadeIn(animTimeInSeconds);
        }
    }
}
