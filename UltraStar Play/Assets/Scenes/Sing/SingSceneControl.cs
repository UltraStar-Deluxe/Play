using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ProTrans;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public VisualTreeAsset sentenceRatingUi;

    [InjectedInInspector]
    public VisualTreeAsset noteUi;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

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

    [Inject(UxmlName = R.UxmlNames.pauseOverlay)]
    private VisualElement pauseOverlay;

    [Inject(UxmlName = R.UxmlNames.doubleClickToTogglePauseElement)]
    private VisualElement doubleClickToTogglePauseElement;

    [Inject(UxmlName = R.UxmlNames.inputLegend)]
    private VisualElement inputLegend;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ThemeManager themeManager;
    
    [Inject]
    private AudioSeparationManager audioSeparationManager;

    public List<PlayerControl> PlayerControls { get; private set; } = new();

    private PlayerControl lastLeadingPlayerControl;

    private VisualElement[] playerUiColumns;

    private SingSceneData sceneData;
    public SingSceneData SceneData
    {
        get
        {
            if (sceneData == null)
            {
                sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SingSceneData>();
            }
            return sceneData;
        }
    }

    public SongMeta SongMeta
    {
        get
        {
            if (SceneData.IsMedley)
            {
                if (sceneData.MedleySongIndex >= sceneData.SongMetas.Count)
                {
                    Debug.LogWarning($"Cannot start medley song at index {sceneData.MedleySongIndex} because there are only {sceneData.SongMetas.Count} songs selected for the medley. Exiting SingScene.");
                    FinishScene(false, false);
                    return null;
                }

                return sceneData.SongMetas[sceneData.MedleySongIndex];
            }

            return SceneData.SongMetas.FirstOrDefault();
        }
    }

    public double DurationOfSongInMillis
    {
        get
        {
            return songAudioPlayer.DurationOfSongInMillis;
        }
    }

    public double PositionInSongInMillis
    {
        get
        {
            return songAudioPlayer.PositionInSongInMillis;
        }
    }

    public double CurrentBeat
    {
        get
        {
            return songAudioPlayer.GetCurrentBeat(false);
        }
    }

    public PartyModeSettings PartyModeSettings => SceneData.partyModeSettings;
    public bool HasPartyModeSettings => PartyModeSettings != null;

    private SingingLyricsControl topSingingLyricsControl;
    private SingingLyricsControl bottomSingingLyricsControl;

    private TimeBarControl timeBarControl;

    private MessageDialogControl dialogControl;
    public bool IsDialogOpen => dialogControl != null;

    private readonly ContextMenuControl contextMenuControl = new();
    private readonly CommonScoreControl commonScoreControl = new();
    private readonly SingSceneCountdownControl countdownControl = new();
    private readonly SingSceneAudioFadeInControl audioFadeInControl = new();
    private readonly SingSceneMedleyControl medleyControl = new();
    private readonly SingScenePartyModeControl partyModeControl = new();

    public bool IsCommonScore => settings.GameSettings.ScoreMode == EScoreMode.CommonAverage
                                 && SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count >= 2;

    public bool IsIndividualScore => settings.GameSettings.ScoreMode == EScoreMode.Individual
                                     || (settings.GameSettings.ScoreMode == EScoreMode.CommonAverage
                                         && SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count <= 1);

    private float startTimeInSeconds;
    private bool hasRecordedSongStartedStatistics;

    public void OnInjectionFinished()
    {
        injector.Inject(commonScoreControl);
        injector.Inject(countdownControl);
        injector.Inject(medleyControl);
        injector.Inject(audioFadeInControl);

        // Register ContextMenu
        injector
            .WithRootVisualElement(doubleClickToTogglePauseElement)
            .Inject(contextMenuControl);
        contextMenuControl.FillContextMenuAction = FillContextMenu;
    }

    private void Start()
    {
        string playerProfilesCsv = SceneData.SingScenePlayerData.SelectedPlayerProfiles.Select(it => it.Name).ToCsv();
        Debug.Log($"{playerProfilesCsv} start (or continue) singing of {SongMeta.Title} at {SceneData.PositionInSongInMillis} ms.");

        startTimeInSeconds = Time.time;

        pauseOverlay.HideByDisplay();
        new DoubleClickControl(doubleClickToTogglePauseElement).DoublePointerDownEventStream
            .Subscribe(_ => TogglePlayPause());

        // Prepare player UI layout (depends on player count)
        PreparePlayerUiLayout();

        // Create PlayerControl (and PlayerUi) for each player
        List<PlayerProfile> playerProfilesWithoutMic = new();
        for (int i = 0; i < SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count; i++)
        {
            PlayerProfile playerProfile = SceneData.SingScenePlayerData.SelectedPlayerProfiles[i];
            SceneData.SingScenePlayerData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            if (micProfile == null)
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }
            PlayerControl playerControl = CreatePlayerControl(playerProfile, micProfile, i);

            if (SceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out List<PlayerScoreControlData> scoreDatas)
                && SceneData.MedleySongIndex < scoreDatas.Count)
            {
                playerControl.PlayerScoreControl.ScoreData = scoreDatas[SceneData.MedleySongIndex];
            }

            // Update leading player icon
            if (SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count > 1)
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
        if (!sceneData.IsMedley || sceneData.MedleySongIndex == 0)
        {
            CreateWarningAboutMissingMicrophonesIfNeeded(playerProfilesWithoutMic);
        }

        webcamControl.InitWebcam();

        // Associate LyricsDisplayer with one of the (duet) players
        InitSingingLyricsControls();

        StartAudioPlayback();
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
        if (SceneData.PositionInSongInMillis <= 0
            && SongMeta.Start > 0)
        {
            // #START tag in txt file is in seconds (but #END is in milliseconds).
            SkipToPositionInSong(SongMeta.Start * 1000);
        }

        // Update TimeBar every second
        if (sceneData.IsMedley)
        {
            StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(1f, () =>
            {
                timeBarControl?.UpdateTimeValueLabel(
                    songAudioPlayer.PositionInSongInMillis - medleyControl.MedleyStartWithCountdownInMillis,
                    medleyControl.MedleyDurationWithCountdownInMillis);
            }));
        }
        else
        {
            StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(1f, () =>
            {
                timeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
            }));
        }

        // Start medley if needed
        if (sceneData.IsMedley)
        {
            medleyControl.StartCurrentMedleySong();
        }

        // Handle party mode
        injector.Inject(partyModeControl);
    }

    private void CreateWarningAboutMissingMicrophonesIfNeeded(List<PlayerProfile> playerProfilesWithoutMic)
    {
        string playerNameCsv = string.Join(", ", playerProfilesWithoutMic.Select(it => it.Name).ToList());
        if (!playerProfilesWithoutMic.IsNullOrEmpty())
        {
            string title = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_title);
            string message = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_message,
                "playerNameCsv", playerNameCsv);

            VisualElement visualElement = dialogUi.CloneTree();
            visualElement.AddToClassList("overlay");
            background.Add(visualElement);

            dialogControl = injector
                .WithRootVisualElement(visualElement)
                .CreateAndInject<MessageDialogControl>();
            dialogControl.Title = title;
            dialogControl.Message = message;
            dialogControl.DialogTitleImage.ShowByDisplay();
            dialogControl.DialogTitleImage.AddToClassList(R.UxmlClasses.warning);
            Button okButton = dialogControl.AddButton("OK", CloseDialog);
            okButton.Focus();

            themeManager.ApplyThemeSpecificStylesToVisualElementsInScene();
        }
    }

    public void OnDestroy()
    {
        webcamControl.Stop();
        audioFadeInControl.Dispose();
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
        int playerCount = SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count;
        playerUiContainer.Children()
            .Where(it => it.name != R.UxmlNames.commonScoreSentenceRatingContainer)
            .ToList()
            .ForEach(it => it.RemoveFromHierarchy());
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
            topLyricsContainer.HideByDisplay();
            bottomLyricsContainer.HideByDisplay();
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
            topLyricsContainer.HideByDisplay();
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

    private void InitTimeBar()
    {
        timeBarControl = new TimeBarControl();
        injector.Inject(timeBarControl);
        timeBarControl.UpdateTimeBarRectangles(SongMeta, PlayerControls, DurationOfSongInMillis);
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
        if (SceneData.IsRestart)
        {
            SceneData.IsRestart = false;
            SceneData.PositionInSongInMillis = 0;
        }
        else
        {
            SceneData.PositionInSongInMillis = PositionInSongInMillis;
        }
    }

    void Update()
    {
        PlayerControls.ForEach(playerControl =>
        {
            if (songAudioPlayer.IsPlaying)
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

        if (sceneData.IsMedley)
        {
            medleyControl.Update();
        }

        partyModeControl.Update();
    }

    public void SkipToNextSingableNote()
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
            return;
        }
        int nextStartBeat = nextSingableNotes.Min();

        // For debugging, go fast to next lyrics. In production, give the player some time to prepare.
        double offsetInMillis = Application.isEditor ? 500 : 1500;
        double targetPositionInMillis = BpmUtils.BeatToMillisecondsInSong(SongMeta, nextStartBeat) - offsetInMillis;
        if (targetPositionInMillis > 0 && targetPositionInMillis > PositionInSongInMillis)
        {
            SkipToPositionInSong(targetPositionInMillis);
        }
    }

    public void SkipToPositionInSong(double positionInSongInMillis)
    {
        int nextBeatToScore = (int)Math.Max(CurrentBeat, sceneData.NextBeatToScore);
        Debug.Log($"Skipping forward to {positionInSongInMillis} milliseconds, next beat to score is {nextBeatToScore}");
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        foreach (PlayerControl playerController in PlayerControls)
        {
            playerController.PlayerScoreControl.NextBeatToScore = nextBeatToScore;
            playerController.PlayerMicPitchTracker.SkipToBeat(CurrentBeat);
        }
    }

    public void Restart()
    {
        SceneData.IsRestart = true;
        sceneNavigator.LoadScene(EScene.SingScene, SceneData);
    }

    public void OpenSongInEditor()
    {
        if (HasPartyModeSettings)
        {
            UiManager.Instance.CreateNotificationVisualElement("Song editor not available in party mode");
            return;
        }
        if (sceneData.IsMedley)
        {
            UiManager.Instance.CreateNotificationVisualElement("Song editor not available during medley");
            return;
        }

        int maxBeatToScore = PlayerControls
            .Select(playerController => playerController.PlayerScoreControl.NextBeatToScore)
            .Max();
        SceneData.NextBeatToScore = Math.Max((int)CurrentBeat, maxBeatToScore);

        SceneData.PlayerProfileToScoreDataMap = new();
        foreach (PlayerControl playerController in PlayerControls)
        {
            SceneData.PlayerProfileToScoreDataMap.Add(playerController.PlayerProfile, new List<PlayerScoreControlData> { playerController.PlayerScoreControl.ScoreData });
        }

        SongEditorSceneData songEditorSceneData = new()
        {
            PreviousSceneData = SceneData,
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
        if (continueWithNextMedleySong
            && sceneData.MedleySongIndex < sceneData.SongMetas.Count - 1)
        {
            StartNextMedleySong();
            return;
        }

        if (settings.GameSettings.ScoreMode == EScoreMode.None)
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
        singingResultsSceneData.partyModeSettings = SceneData.partyModeSettings;

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
            EAvatar commonPlayerProfileAvatar = PlayerControls.FirstOrDefault().PlayerProfile.Avatar;
            PlayerProfile commonPlayerProfile = new(commonPlayerProfileName, easiestPlayerProfileDifficulty, commonPlayerProfileAvatar);
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
            || HasPartyModeSettings)
        {
            // Medleys and party mode are not recorded
            return;
        }
        statistics.RecordSongFinished(SongMeta, songStatistics);
    }

    private PlayerControl CreatePlayerControl(PlayerProfile playerProfile, MicProfile micProfile, int playerIndex)
    {
        Voice voice = GetVoice(playerProfile);

        PlayerControl playerControl = GameObject.Instantiate<PlayerControl>(playerControlPrefab);

        Injector playerControlInjector = UniInjectUtils.CreateInjector(injector);
        playerControlInjector.AddBindingForInstance(playerProfile);
        playerControlInjector.AddBindingForInstance(voice);
        playerControlInjector.AddBindingForInstance(micProfile);
        playerControlInjector.AddBindingForInstance(playerControlInjector, RebindingBehavior.Ignore);
        playerControlInjector.Inject(playerControl);

        PlayerControls.Add(playerControl);

        AddPlayerUi(playerControl.PlayerUiControl.RootVisualElement, playerIndex);

        return playerControl;
    }

    private void AddPlayerUi(VisualElement visualElement, int playerIndex)
    {
        int playerCount = SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count;
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

        if (sceneData.SingScenePlayerData.PlayerProfileToVoiceNameMap.TryGetValue(playerProfile, out string voiceName))
        {
            return voiceName;
        }

        if (SceneData.SingScenePlayerData.SelectedPlayerProfiles.Count == 1)
        {
            return Voice.soloVoiceName;
        }

        int voiceIndex = SceneData.SingScenePlayerData.SelectedPlayerProfiles.IndexOf(playerProfile) % voiceNames.Count;
        return voiceNames[voiceIndex];
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            pauseOverlay.ShowByDisplay();
            songAudioPlayer.PauseAudio();
            PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
        }
        else
        {
            pauseOverlay.HideByDisplay();
            songAudioPlayer.PlayAudio();
            PlayerControls.ForEach(playerControl =>
            {
                playerControl.PlayerMicPitchTracker.SendPositionInSongToClientRapidly();
                playerControl.PlayerMicPitchTracker.SendStartRecordingMessageToConnectedClient();
            });
        }
    }

    private void StartAudioPlayback()
    {
        if (songAudioPlayer.IsPlaying)
        {
            Debug.LogWarning("Song already playing");
        }

        songAudioPlayer.Init(SongMeta);

        if (!songAudioPlayer.HasAudioClip)
        {
            // Loading the audio failed.
            PlayerControls.ForEach(playerControl => playerControl.PlayerMicPitchTracker.SendStopRecordingMessageToConnectedClient());
            sceneNavigator.LoadScene(EScene.SongSelectScene);
        }

        // The time bar needs the duration of the song to calculate positions.
        // The duration of the song should be available now.
        InitTimeBar();

        songAudioPlayer.PlayAudio();
        if (SceneData.PositionInSongInMillis > 0)
        {
            SkipToPositionInSong(SceneData.PositionInSongInMillis);
        }
    }

    public List<IBinding> GetBindings()
    {
        // Binding happens before the injection finished. Thus, no fields can be used here that have been injected.
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(singSceneFinisher);
        bb.BindExistingInstance(countdownControl);
        bb.BindExistingInstance(medleyControl);
        bb.BindExistingInstance(audioFadeInControl);
        bb.BindExistingInstance(partyModeControl);
        bb.BindExistingInstance(alternativeAudioPlayer);
        bb.Bind(nameof(playerUi)).ToExistingInstance(playerUi);
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
        Voice matchingVoice = voices.FirstOrDefault(it => it.VoiceNameEquals(voiceName));
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
        inputLegend.Query<Label>()
            .Where(label => label is not FontIcon)
            .ForEach(label => label.RemoveFromHierarchy());

        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_back,
            TranslationManager.GetTranslation(R.Messages.back),
            inputLegend);
        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_openSongEditor,
            TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            inputLegend);
        InputLegendControl.TryAddInputActionInfo(R.InputActions.usplay_restartSong,
            TranslationManager.GetTranslation(R.Messages.action_restart),
            inputLegend);

        if (inputManager.InputDeviceEnum == EInputDevice.Touch)
        {
            inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
                TranslationManager.GetTranslation(R.Messages.continue_),
                TranslationManager.GetTranslation(R.Messages.action_doubleTap))));
            inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
                TranslationManager.GetTranslation(R.Messages.action_openContextMenu),
                TranslationManager.GetTranslation(R.Messages.action_longPress))));
        }
        else
        {
            inputLegend.Add(InputLegendControl.CreateInputActionInfoUi(new InputActionInfo(
                TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
                TranslationManager.GetTranslation(R.Messages.action_navigateRight))));
        }
    }

    public void CloseDialog()
    {
        if (dialogControl == null)
        {
            return;
        }

        dialogControl.CloseDialog();
        dialogControl = null;
    }

    protected void FillContextMenu(ContextMenuPopupControl contextMenuPopup)
    {
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_togglePause),
            () => TogglePlayPause());

        webcamControl.AddToContextMenu(contextMenuPopup);
        
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_restart),
            () => Restart());
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_skipToNextLyrics),
            () => SkipToNextSingableNote());
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_exitSong),
            () => FinishScene(false, false));
        contextMenuPopup.AddButton(TranslationManager.GetTranslation(R.Messages.action_openSongEditor),
            () => OpenSongInEditor());

        contextMenuPopup.AddSeparator();

        // Button to separate audio or slider to change vocals audio
        if (SongMetaUtils.VocalsAudioResourceExists(SongMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(SongMeta))
        {
            contextMenuPopup.AddVisualElement(new Label("Vocals Volume"));
            Slider vocalsVolumeSlider = new();
            vocalsVolumeSlider.lowValue = 0;
            vocalsVolumeSlider.highValue = 100;
            vocalsVolumeSlider.value = settings.AudioSettings.VocalsAudioVolumePercent;
            vocalsVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                settings.AudioSettings.VocalsAudioVolumePercent = (int)evt.newValue;
            });

            contextMenuPopup.AddVisualElement(vocalsVolumeSlider);
        }
        else
        {
            contextMenuPopup.AddButton("Separate audio",
                () => audioSeparationManager.ProcessSongMeta(SongMeta));
        }
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
