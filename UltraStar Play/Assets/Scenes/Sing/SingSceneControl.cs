using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using ProTrans;
using UniInject.Extensions;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using IBinding = UniInject.IBinding;
using Image = UnityEngine.UI.Image;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneControl : MonoBehaviour, INeedInjection, IBinder
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
    public VisualTreeAsset dialogUi;

    [Inject(UxmlName = R.UxmlNames.background)]
    public VisualElement background;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [Inject]
    private Injector sceneInjector;

    [Inject]
    private Statistics statistics;

    [Inject(UxmlName = R.UxmlNames.topLyricsContainer)]
    private VisualElement topLyricsContainer;

    [Inject(UxmlName = R.UxmlNames.bottomLyricsContainer)]
    private VisualElement bottomLyricsContainer;

    [Inject(UxmlName = R.UxmlNames.pauseOverlay)]
    private VisualElement pauseOverlay;

    public List<PlayerControl> PlayerControllers { get; private set; } = new List<PlayerControl>();

    public List<AbstractDummySinger> DummySingers { get; private set; } = new List<AbstractDummySinger>();

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
            return SceneData.SelectedSongMeta;
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
            return songAudioPlayer.CurrentBeat;
        }
    }

    private SingingLyricsControl topSingingLyricsControl;
    private SingingLyricsControl bottomSingingLyricsControl;

    private TimeBarControl timeBarControl;

    void Start()
    {
        string playerProfilesCsv = SceneData.SelectedPlayerProfiles.Select(it => it.Name).ToCsv();
        Debug.Log($"{playerProfilesCsv} start (or continue) singing of {SongMeta.Title} at {SceneData.PositionInSongInMillis} ms.");

        pauseOverlay.HideByDisplay();

        // Handle players
        List<PlayerProfile> playerProfilesWithoutMic = new List<PlayerProfile>();
        foreach (PlayerProfile playerProfile in SceneData.SelectedPlayerProfiles)
        {
            SceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            if (micProfile == null)
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }
            PlayerControl playerControl = CreatePlayerControl(playerProfile, micProfile);

            if (SceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out PlayerScoreControllerData scoreData))
            {
                playerControl.PlayerScoreController.ScoreData = scoreData;
            }

            // Handle crown display
            if (SceneData.SelectedPlayerProfiles.Count > 1)
            {
                playerControl.PlayerScoreController.NoteScoreEventStream.Subscribe(noteScoreEvent => { RecomputeCrowns(); });
                playerControl.PlayerScoreController.SentenceScoreEventStream.Subscribe(sentenceScoreEvent => { RecomputeCrowns(); });
            }
        }

        // Handle dummy singers
        if (Application.isEditor)
        {
            DummySingers = FindObjectsOfType<AbstractDummySinger>().ToList();
            foreach (AbstractDummySinger dummySinger in DummySingers)
            {
                if (dummySinger.playerIndexToSimulate < PlayerControllers.Count)
                {
                    dummySinger.SetPlayerController(PlayerControllers[dummySinger.playerIndexToSimulate]);
                }
                else
                {
                    Debug.LogWarning("DummySinger cannot simulate player with index " + dummySinger.playerIndexToSimulate);
                    dummySinger.gameObject.SetActive(false);
                }
            }
        }

        // Create warning about missing microphones
        string playerNameCsv = string.Join(",", playerProfilesWithoutMic.Select(it => it.Name).ToList());
        if (!playerProfilesWithoutMic.IsNullOrEmpty())
        {
            string title = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_title);
            string message = TranslationManager.GetTranslation(R.Messages.singScene_missingMicrophones_message,
                "playerNameCsv", playerNameCsv);
            SimpleDialogControl dialogControlControl = new SimpleDialogControl(dialogUi, background, title, message);
            dialogControlControl.DialogTitleImage.ShowByDisplay();
            dialogControlControl.DialogTitleImage.AddToClassList(R.UxmlClasses.warning);
            Button okButton = dialogControlControl.AddButton("OK", dialogControlControl.CloseDialog);
            okButton.Focus();
        }

        // Associate LyricsDisplayer with one of the (duett) players
        InitSingingLyricsControls();

        //Save information about the song being started into stats
        Statistics stats = StatsManager.Instance.Statistics;
        stats.RecordSongStarted(SongMeta);

        songVideoPlayer.Init(SongMeta, songAudioPlayer);

        StartCoroutine(StartMusicAndVideo());

        // Update TimeBar every second
        StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(1f, () =>
        {
            timeBarControl?.UpdateTimeValueLabel(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        }));

        // Rebuild whole UI
        LayoutRebuilder.ForceRebuildLayoutImmediate(CanvasUtils.FindCanvas().GetComponent<RectTransform>());
    }

    private void InitSingingLyricsControls()
    {
        if (PlayerControllers.IsNullOrEmpty())
        {
            return;
        }

        SingingLyricsControl CreateSingingLyricsControl(VisualElement visualElement, PlayerControl playerController)
        {
            SingingLyricsControl singingLyricsControl = new SingingLyricsControl();
            Injector lyricsControlInjector = UniInjectUtils.CreateInjector(sceneInjector);
            lyricsControlInjector.AddBindingForInstance(playerController);
            lyricsControlInjector.WithRootVisualElement(visualElement).Inject(singingLyricsControl);
            return singingLyricsControl;
        }

        bool needSecondLyricsDisplayer = SongMeta.GetVoices().Count > 1
                                         && PlayerControllers.Count > 1;
        if (needSecondLyricsDisplayer)
        {
            topSingingLyricsControl = CreateSingingLyricsControl(topLyricsContainer, PlayerControllers[0]);
            bottomSingingLyricsControl = CreateSingingLyricsControl(bottomLyricsContainer, PlayerControllers[1]);
        }
        else
        {
            topLyricsContainer.HideByDisplay();
            bottomSingingLyricsControl = CreateSingingLyricsControl(bottomLyricsContainer, PlayerControllers[0]);
        }
    }

    private void RecomputeCrowns()
    {
        // Find best player with score > 0
        PlayerControl bestScorePlayerControl = null;
        foreach (PlayerControl playerController in PlayerControllers)
        {
            if ((bestScorePlayerControl == null && playerController.PlayerScoreController.TotalScore > 0)
               || (bestScorePlayerControl != null && playerController.PlayerScoreController.TotalScore > bestScorePlayerControl.PlayerScoreController.TotalScore))
            {
                bestScorePlayerControl = playerController;
            }
        }

        // // Show crown on best player
        if (bestScorePlayerControl != null)
        {
            bestScorePlayerControl.PlayerUiControl.ShowLeadingPlayerIcon();
        }
        // Hide crown on other players
        foreach (PlayerControl playerController in PlayerControllers)
        {
            if (playerController != bestScorePlayerControl)
            {
                playerController.PlayerUiControl.HideLeadingPlayerIcon();
            }
        }
    }

    private void InitTimeBar()
    {
        timeBarControl = new TimeBarControl();
        sceneInjector.Inject(timeBarControl);
        timeBarControl.UpdateTimeBarRectangles(SongMeta, PlayerControllers, DurationOfSongInMillis);
    }

    private IEnumerator StartMusicAndVideo()
    {
        // Start the music
        yield return StartAudioPlayback();

        // Start any associated video
        if (string.IsNullOrEmpty(SongMeta.Video))
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
        PlayerControllers.ForEach(it => it.SetCurrentBeat(CurrentBeat));
        timeBarControl.UpdatePositionIndicator(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis);
        topSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);
        bottomSingingLyricsControl?.Update(songAudioPlayer.PositionInSongInMillis);
    }

    public void SkipToNextSingableNote()
    {
        IEnumerable<int> nextSingableNotes = PlayerControllers
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
        foreach (PlayerControl playerController in PlayerControllers)
        {
            playerController.PlayerScoreController.NextBeatToScore = nextBeatToScore;
            playerController.PlayerPitchTracker.SkipToBeat(CurrentBeat);
        }
    }

    public void Restart()
    {
        SceneData.IsRestart = true;
        SceneNavigator.Instance.LoadScene(EScene.SingScene, SceneData);
    }

    public void OpenSongInEditor()
    {
        int maxBeatToScore = PlayerControllers
            .Select(playerController => playerController.PlayerScoreController.NextBeatToScore)
            .Max();
        SceneData.NextBeatToScore = Math.Max((int)CurrentBeat, maxBeatToScore);

        SceneData.PlayerProfileToScoreDataMap = new Dictionary<PlayerProfile, PlayerScoreControllerData>();
        foreach (PlayerControl playerController in PlayerControllers)
        {
            SceneData.PlayerProfileToScoreDataMap.Add(playerController.PlayerProfile, playerController.PlayerScoreController.ScoreData);
        }

        SongEditorSceneData songEditorSceneData = new SongEditorSceneData();
        songEditorSceneData.PreviousSceneData = SceneData;
        songEditorSceneData.PreviousScene = EScene.SingScene;
        songEditorSceneData.PositionInSongInMillis = PositionInSongInMillis;
        songEditorSceneData.SelectedSongMeta = SongMeta;
        songEditorSceneData.PlayerProfileToMicProfileMap = sceneData.PlayerProfileToMicProfileMap;
        songEditorSceneData.SelectedPlayerProfiles = sceneData.SelectedPlayerProfiles;
        SceneNavigator.Instance.LoadScene(EScene.SongEditorScene, songEditorSceneData);
    }

    public void FinishScene(bool isAfterEndOfSong)
    {
        // Open the singing results scene.
        SingingResultsSceneData singingResultsSceneData = new SingingResultsSceneData();
        singingResultsSceneData.SongMeta = SongMeta;
        singingResultsSceneData.PlayerProfileToMicProfileMap = sceneData.PlayerProfileToMicProfileMap;
        singingResultsSceneData.SongDurationInMillis = (int)songAudioPlayer.DurationOfSongInMillis;

        // Check if the full song has been sung, i.e., the playback position is after the last note.
        // This determines whether the statistics should be updated and the score should be recorded.
        bool isAfterLastNote = true;
        foreach (PlayerControl playerController in PlayerControllers)
        {
            singingResultsSceneData.AddPlayerScores(playerController.PlayerProfile, playerController.PlayerScoreController.ScoreData);

            Note lastNoteInSong = playerController.GetLastNoteInSong();
            if (!isAfterEndOfSong && CurrentBeat < lastNoteInSong.EndBeat)
            {
                isAfterLastNote = false;
            }
        }

        if (isAfterLastNote)
        {
            UpdateSongFinishedStats();
        }

        SceneNavigator.Instance.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private void UpdateSongFinishedStats()
    {
        List<SongStatistic> songStatistics = PlayerControllers
            .Select(playerController => new SongStatistic(playerController.PlayerProfile.Name,
                                                          playerController.PlayerProfile.Difficulty,
                                                          playerController.PlayerScoreController.TotalScore))
            .ToList();
        statistics.RecordSongFinished(SongMeta, songStatistics);
    }

    private PlayerControl CreatePlayerControl(PlayerProfile playerProfile, MicProfile micProfile)
    {
        Voice voice = GetVoice(playerProfile);

        PlayerControl playerControl = GameObject.Instantiate<PlayerControl>(playerControlPrefab);

        Injector playerControlInjector = UniInjectUtils.CreateInjector(sceneInjector);
        playerControlInjector.AddBindingForInstance(playerProfile);
        playerControlInjector.AddBindingForInstance(voice);
        playerControlInjector.AddBindingForInstance(micProfile);
        playerControlInjector.AddBindingForInstance(playerControlInjector, RebindingBehavior.Ignore);
        playerControlInjector.Inject(playerControl);

        PlayerControllers.Add(playerControl);
        return playerControl;
    }

    private string GetVoiceName(PlayerProfile playerProfile)
    {
        List<string> voiceNames = new List<string>(SongMeta.VoiceNames.Keys);
        int voiceNameCount = voiceNames.Count;
        if (voiceNameCount <= 1)
        {
            return Voice.soloVoiceName;
        }

        if (sceneData.PlayerProfileToVoiceNameMap.TryGetValue(playerProfile, out string voiceName))
        {
            return voiceName;
        }

        if (SceneData.SelectedPlayerProfiles.Count == 1)
        {
            return Voice.soloVoiceName;
        }

        int voiceIndex = SceneData.SelectedPlayerProfiles.IndexOf(playerProfile) % voiceNames.Count;
        return voiceNames[voiceIndex];
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            pauseOverlay.ShowByDisplay();
            songAudioPlayer.PauseAudio();
        }
        else
        {
            pauseOverlay.HideByDisplay();
            songAudioPlayer.PlayAudio();
        }
    }

    private IEnumerator StartAudioPlayback()
    {
        if (songAudioPlayer.IsPlaying)
        {
            Debug.LogWarning("Song already playing");
            yield break;
        }

        songAudioPlayer.Init(SongMeta);

        if (!songAudioPlayer.HasAudioClip)
        {
            // Loading the audio failed.
            SceneNavigator.Instance.LoadScene(EScene.SongSelectScene);
            yield break;
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
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.Bind("playerUi").ToExistingInstance(playerUi);
        return bb.GetBindings();
    }

    private Voice GetVoice(PlayerProfile playerProfile)
    {
        string voiceName = GetVoiceName(playerProfile);
        IReadOnlyCollection<Voice> voices = sceneData.SelectedSongMeta.GetVoices();
        Voice matchingVoice = voices.FirstOrDefault(it => it.Name == voiceName);
        if (matchingVoice != null)
        {
            return matchingVoice;
        }

        string voiceNameCsv = voices.Select(it => it.Name).ToCsv();
        Debug.LogError($"The song data does not contain a voice with name {voiceName}."
                       + $" Available voice names: {voiceNameCsv}");
        return voices.FirstOrDefault();
    }
}
