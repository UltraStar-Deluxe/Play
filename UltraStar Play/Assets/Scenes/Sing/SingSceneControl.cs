using System;
using System.Collections.Generic;
using System.Linq;
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

    [Inject(UxmlName = R.UxmlNames.songTimeProgressBar)]
    private ProgressBar songTimeProgressBar;

    [Inject(UxmlClass = R.UssClasses.playerInfoUiList)]
    private List<VisualElement> playerInfoUiLists;
    
    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ThemeManager themeManager;
    
    [Inject]
    private AudioSeparationManager audioSeparationManager;

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
                                sceneData.gameRoundSettings.modifiers.Contains(EGameRoundModifier.PassTheMic);

    private SingingLyricsControl topSingingLyricsControl;
    private SingingLyricsControl bottomSingingLyricsControl;

    private readonly TimeBarControl timeBarControl = new();

    private MessageDialogControl dialogControl;

    private readonly SingSceneGovernanceControl singSceneGovernanceControl = new();
    private readonly CommonScoreControl commonScoreControl = new();
    private readonly SingSceneCountdownControl countdownControl = new();
    private readonly SingSceneAudioFadeInControl audioFadeInControl = new();
    private readonly SingSceneMedleyControl medleyControl = new();
    private readonly SingSceneModifierControl modifierControl = new();

    public bool IsCommonScore => settings.GameSettings.ScoreMode == EScoreMode.CommonAverage
                                 && sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 2;

    public bool IsIndividualScore => settings.GameSettings.ScoreMode == EScoreMode.Individual
                                     || (settings.GameSettings.ScoreMode == EScoreMode.CommonAverage
                                         && sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count <= 1);

    private float startTimeInSeconds;
    private bool hasRecordedSongStartedStatistics;

    private bool hasFinishedScene;

    private float startMicrophoneDelayInSeconds = 0.5f;
    
    public void OnInjectionFinished()
    {
        injector.Inject(timeBarControl);
        injector.Inject(commonScoreControl);
        injector.Inject(countdownControl);
        injector.Inject(medleyControl);
        injector.Inject(audioFadeInControl);
        injector.Inject(modifierControl);
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
            if (micProfile == null)
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }
            PlayerControl playerControl = CreatePlayerControl(playerProfile, micProfile, i);

            if (sceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out List<PlayerScoreControlData> scoreDatas)
                && sceneData.MedleySongIndex < scoreDatas.Count
                && sceneData.MedleySongIndex >= 0)
            {
                playerControl.PlayerScoreControl.ScoreData = scoreDatas[sceneData.MedleySongIndex];
            }

            // Update leading player icon
            if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 1)
            {
                playerControl.PlayerScoreControl.SentenceScoreEventStream
                    .Subscribe(_ => UpdateLeadingPlayerIcon());
            }
        }

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

        // Start the audio when microphones are ready.
        if (sceneData.IsMedley)
        {
            // No time to wait
            StartAudioPlayback();
        }
        else
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(startMicrophoneDelayInSeconds, 
                () => StartAudioPlayback()));
        }
        StartVideoOrShowBackgroundImage();

        // Input legend (in pause overlay)
        UpdateInputLegend();
        inputManager.InputDeviceChangeEventStream.Subscribe(_ => UpdateInputLegend());

        // Automatically start recording on companion apps
        PlayerControls.ForEach(playerControl =>
        {
            playerControl.PlayerMicPitchTracker.SendMicProfileToConnectedClient();
            playerControl.PlayerMicPitchTracker.SendStartRecordingMessageToConnectedClient();
        });

        // Skip beginning of song via #START tag of txt file
        if (sceneData.PositionInSongInMillis <= 0
            && SongMeta.Start > 0)
        {
            // #START tag in txt file is in seconds (but #END is in milliseconds).
            SkipToPositionInSong(SongMeta.Start * 1000);
        }

        // Progress bar to show time in song
        songTimeProgressBar.value = 0;
        songAudioPlayer.PositionInSongEventStream.Subscribe(_ =>
        {
            double progressInPercent = 100 * (songAudioPlayer.PositionInSongInMillis / songAudioPlayer.DurationOfSongInMillis);
            songTimeProgressBar.value = (float) progressInPercent;
        });
        settings.ObserveEveryValueChanged(it => it.GraphicSettings.showSongProgress)
            .Subscribe(newValue => songTimeProgressBar.SetVisibleByDisplay(newValue));

        // Update TimeBar every second
        StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(1f, () =>
        {
            timeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        }));
        
        // Start medley if needed
        if (sceneData.IsMedley)
        {
            medleyControl.StartCurrentMedleySong();
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

    private void InitSingingLyricsControls()
    {
        if (PlayerControls.IsNullOrEmpty()
            || !settings.GraphicSettings.showStaticLyrics)
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

        Dictionary<Voice, List<PlayerControl>> voiceToPlayerControlsMap = new();
        PlayerControls.ForEach(it => voiceToPlayerControlsMap.AddInsideList(it.Voice, it));
        if (voiceToPlayerControlsMap.Keys.Count >= 2)
        {
            // There are two different sets of lyrics that need to be displayed
            List<PlayerControl> playerControlsUsingFirstVoice = voiceToPlayerControlsMap[voiceToPlayerControlsMap.Keys.FirstOrDefault()];
            List<PlayerControl> playerControlsUsingSecondVoice = voiceToPlayerControlsMap[voiceToPlayerControlsMap.Keys.LastOrDefault()];
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
            bottomSingingLyricsControl = CreateSingingLyricsControl(bottomLyricsContainer, PlayerControls[0]);
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
        songVideoPlayer.SongMeta = SongMeta;
        if (SongMeta.Video.IsNullOrEmpty())
        {
            songVideoPlayer.ShowBackgroundImage();
        }
        else
        {
            songVideoPlayer.StartVideoOrShowBackgroundImage();
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
        timeBarControl.UpdatePositionIndicator(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        topSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);
        bottomSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);

        if (!hasRecordedSongStartedStatistics)
        {
            // Save information that the song has been started after some seconds or half of the song.
            float songSingingDuration = Time.time - startTimeInSeconds;
            float songDurationInSeconds = (float)songAudioPlayer.DurationOfSongInMillis / 1000;
            if (songSingingDuration >= 30
                || (songDurationInSeconds > 0
                    && songSingingDuration >= songDurationInSeconds / 2))
            {
                hasRecordedSongStartedStatistics = true;
                statistics.RecordSongStarted(SongMeta);
            }
        }
        
        singSceneGovernanceControl.Update();

        if (sceneData.IsMedley)
        {
            medleyControl.Update();
        }

        modifierControl.Update();

        if (!IsPaused)
        {
            countdownControl.Update(Time.deltaTime);
        }
    }

    public void SkipToNextSingableNoteOrEndOfSong()
    {
        if (sceneData.IsMedley)
        {
            // Skipping is not allowed in a medley.
            return;
        }

        IEnumerable<int> nextSingableNotes = PlayerControls
            .Select(it => it.GetNextSingableNote(CurrentBeat))
            .Where(nextSingableNote => nextSingableNote != null)
            .Select(nextSingableNote => nextSingableNote.StartBeat);
        if (nextSingableNotes.Count() <= 0)
        {
            SkipToEndOfSong();
            return;
        }
        int nextStartBeat = nextSingableNotes.Min();

        // For debugging, go fast to next lyrics. In production, give the player some time to prepare.
        double offsetInMillis = Application.isEditor ? 500 : 2000;
        double targetPositionInMillis = BpmUtils.BeatToMillisecondsInSong(SongMeta, nextStartBeat) - offsetInMillis;
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
        int nextBeatToScore = (int)Math.Max(CurrentBeat, sceneData.NextBeatToScore);
        foreach (PlayerControl playerController in PlayerControls)
        {
            playerController.PlayerScoreControl.NextBeatToScore = nextBeatToScore;
            playerController.PlayerMicPitchTracker.SkipToBeat(CurrentBeat);
        }
        Debug.Log($"Skipped forward to {positionInSongInMillis} milliseconds, next beat to score is {nextBeatToScore}");
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
            UiManager.CreateNotification("Song editor not available in party mode");
            return;
        }
        if (sceneData.IsMedley)
        {
            UiManager.CreateNotification("Song editor not available during medley");
            return;
        }

        int maxBeatToScore = PlayerControls
            .Select(playerController => playerController.PlayerScoreControl.NextBeatToScore)
            .Max();
        sceneData.NextBeatToScore = Math.Max((int)CurrentBeat, maxBeatToScore);

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

        if (settings.GameSettings.ScoreMode == EScoreMode.None
            && !HasPartyModeSceneData)
        {
            FinishSceneToSongSelect();
        }
        else
        {
            FinishSceneToSingingResults(isAfterEndOfSong);
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

        // Add scores, either for individual players, or as one common score.
        List<SongStatistic> songStatistics = new();
        if (IsIndividualScore)
        {
            // Add and record score for each player individually.
            singingResultsSceneData.PlayerProfileToMicProfileMap = sceneData.SingScenePlayerData.PlayerProfileToMicProfileMap;
            PlayerControls.ForEach(playerControl =>
            {
                PlayerScoreControlData playerScoreControlData = GetPlayerScoreDataForSingingResultsScene(playerControl);
                singingResultsSceneData.AddPlayerScores(playerControl.PlayerProfile, playerScoreControlData);
            });

            songStatistics = PlayerControls
                .Select(playerControl => new SongStatistic(playerControl.PlayerProfile.Name,
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
                .JoinWith(settings.GameSettings.CommonScoreNameSeparator);
            EDifficulty easiestPlayerProfileDifficulty = PlayerControls
                .FindMinElement(playerControl => (int)playerControl.PlayerProfile.Difficulty)
                .PlayerProfile.Difficulty;
            string commonProfileImagePath = PlayerControls.FirstOrDefault().PlayerProfile.ImagePath;
            PlayerProfile commonPlayerProfile = new(commonPlayerProfileName, easiestPlayerProfileDifficulty, commonProfileImagePath);
            PlayerScoreControlData commonScoreData = CreateAveragePlayerScoreControlData(scoreControlDatas);
            singingResultsSceneData.AddPlayerScores(commonPlayerProfile, commonScoreData);

            SongStatistic commonSongStatistic = new SongStatistic(
                commonPlayerProfileName,
                easiestPlayerProfileDifficulty,
                commonScoreData.TotalScore,
                EScoreMode.CommonAverage);
            songStatistics = new() { commonSongStatistic };
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
        if (isAfterLastNote
            && !songStatistics.IsNullOrEmpty())
        {
            UpdateSongFinishedStats(songStatistics);
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

    private void UpdateSongFinishedStats(List<SongStatistic> songStatistics)
    {
        if (sceneData.IsMedley
            || HasPartyModeSceneData)
        {
            // Medleys and party mode are not recorded
            return;
        }
        statistics.RecordSongFinished(SongMeta, songStatistics);
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

        // Start microphone after a short delay. Otherwise the scene transition is not smooth.
        if (sceneData.IsMedley)
        {
            // No time to wait
            playerControl.PlayerMicPitchTracker.InitPitchDetection();
        }
        else
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(startMicrophoneDelayInSeconds,
                () => playerControl.PlayerMicPitchTracker.InitPitchDetection()));
        }

        AddPlayerUi(playerControl.PlayerUiControl.RootVisualElement, playerIndex);

        return playerControl;
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

    private string GetVoiceName(PlayerProfile playerProfile)
    {
        List<string> voiceNames = new(SongMeta.VoiceNames.Keys);
        int voiceNameCount = voiceNames.Count;
        if (voiceNameCount <= 1)
        {
            return Voice.soloVoiceName;
        }

        if (sceneData.SingScenePlayerData.PlayerProfileToVoiceNameMap.TryGetValue(playerProfile, out string voiceNameOrPerformerName))
        {
            // The given value could be "P1" / "P2" (i.e. a voiceName) or the performer's name (e.g. "Elvis").
            string matchingVoiceName = SongMeta.VoiceNames
                .Where(entry => entry.Key == voiceNameOrPerformerName
                    || entry.Value == voiceNameOrPerformerName)
                .Select(entry => entry.Key)
                .FirstOrDefault();
            return matchingVoiceName;
        }

        if (sceneData.SingScenePlayerData.SelectedPlayerProfiles.Count == 1)
        {
            return Voice.soloVoiceName;
        }

        int voiceIndex = sceneData.SingScenePlayerData.SelectedPlayerProfiles.IndexOf(playerProfile) % voiceNames.Count;
        return voiceNames[voiceIndex];
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }
        
        songAudioPlayer.PauseAudio();
        PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
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
            playerControl.PlayerMicPitchTracker.SendPositionInSongToClientRapidly();
            playerControl.PlayerMicPitchTracker.SendStartRecordingMessageToConnectedClient();
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
        }

        songAudioPlayer.Init(SongMeta);

        if (!songAudioPlayer.IsPartiallyLoaded)
        {
            // Loading the audio failed.
            PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
            sceneNavigator.LoadScene(EScene.SongSelectScene);
        }

        // The time bar needs the duration of the song to calculate positions.
        // The duration of the song should be available now.
        timeBarControl.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationOfSongInMillis);
        songAudioPlayer.LoadedEventStream.Subscribe(_ => timeBarControl.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationOfSongInMillis));

        songAudioPlayer.PlayAudio();
        if (sceneData.PositionInSongInMillis > 0)
        {
            SkipToPositionInSong(sceneData.PositionInSongInMillis);
        }
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
        bb.BindExistingInstance(modifierControl);
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
        string voiceName = GetVoiceName(playerProfile);
        IReadOnlyCollection<Voice> voices = SongMeta.GetVoices();
        Voice matchingVoice = voices.FirstOrDefault(it => Voice.VoiceNameEquals(it.Name, voiceName));
        if (matchingVoice != null)
        {
            return matchingVoice;
        }

        string voiceNameCsv = voices.Select(it => it.Name).ToCsv();
        Debug.LogError($"The song data does not contain a voice with name {voiceName}."
                       + $" Available voice names: {voiceNameCsv}");
        return voices.FirstOrDefault();
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
