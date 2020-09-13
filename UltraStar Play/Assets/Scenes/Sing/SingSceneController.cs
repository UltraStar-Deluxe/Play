using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneController : MonoBehaviour, INeedInjection, IBinder, IOnHotSwapFinishedListener
{
    private static SingSceneController instance;
    public static SingSceneController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SingSceneController>();
            }
            return instance;
        }
    }

    [InjectedInInspector]
    public GameObject pauseOverlay;

    [InjectedInInspector]
    public PlayerController playerControllerPrefab;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public PlayerUiArea playerUiArea;

    [Inject]
    private Injector sceneInjector;

    [Inject]
    private Statistics statistics;

    public List<PlayerController> PlayerControllers { get; private set; } = new List<PlayerController>();

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

    void Start()
    {
        string playerProfilesCsv = SceneData.SelectedPlayerProfiles.Select(it => it.Name).ToCsv();
        Debug.Log($"{playerProfilesCsv} start (or continue) singing of {SongMeta.Title} at {SceneData.PositionInSongInMillis} ms.");

        // Prepare columns and rows for player UI
        playerUiArea.SetPlayerCount(SceneData.SelectedPlayerProfiles.Count);

        // Handle players
        List<PlayerProfile> playerProfilesWithoutMic = new List<PlayerProfile>();
        foreach (PlayerProfile playerProfile in SceneData.SelectedPlayerProfiles)
        {
            SceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            if (micProfile == null)
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }
            PlayerController playerController = CreatePlayerController(playerProfile, micProfile);

            if (SceneData.PlayerProfileToScoreDataMap.TryGetValue(playerProfile, out PlayerScoreControllerData scoreData))
            {
                playerController.PlayerScoreController.ScoreData = scoreData;
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
            UiManager.Instance.CreateWarningDialog("Missing microphones", $"No microphone for player(s) {playerNameCsv}");
        }

        // Associate LyricsDisplayer with one of the (duett) players
        if (!PlayerControllers.IsNullOrEmpty())
        {
            LyricsDisplayer lyricsDisplayer = FindObjectOfType<LyricsDisplayer>();
            lyricsDisplayer?.Init(PlayerControllers[0]);
        }

        //Save information about the song being started into stats
        Statistics stats = StatsManager.Instance.Statistics;
        stats.RecordSongStarted(SongMeta);

        songVideoPlayer.Init(SongMeta, songAudioPlayer);

        StartCoroutine(StartMusicAndVideo());
    }

    private void InitTimeBar()
    {
        TimeBarTimeLine timeBarTimeLine = FindObjectOfType<TimeBarTimeLine>();
        timeBarTimeLine.Init(SongMeta, PlayerControllers, DurationOfSongInMillis);
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

    public void OnHotSwapFinished()
    {
        StartMusicAndVideo();
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
        foreach (PlayerController playerController in PlayerControllers)
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
        foreach (PlayerController playerController in PlayerControllers)
        {
            SceneData.PlayerProfileToScoreDataMap.Add(playerController.PlayerProfile, playerController.PlayerScoreController.ScoreData);
        }

        SongEditorSceneData songEditorSceneData = new SongEditorSceneData();
        songEditorSceneData.PreviousSceneData = SceneData;
        songEditorSceneData.PreviousScene = EScene.SingScene;
        songEditorSceneData.PositionInSongInMillis = PositionInSongInMillis;
        songEditorSceneData.SelectedSongMeta = SongMeta;
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
        foreach (PlayerController playerController in PlayerControllers)
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

    private PlayerController CreatePlayerController(PlayerProfile playerProfile, MicProfile micProfile)
    {
        string voiceName = GetVoiceName(playerProfile);
        PlayerController playerController = GameObject.Instantiate<PlayerController>(playerControllerPrefab);
        sceneInjector.Inject(playerController);
        playerController.Init(playerProfile, voiceName, micProfile);

        PlayerControllers.Add(playerController);
        return playerController;
    }

    private string GetVoiceName(PlayerProfile playerProfile)
    {
        if (SceneData.SelectedPlayerProfiles.Count == 1)
        {
            return Voice.soloVoiceName;
        }

        List<string> voiceNames = new List<string>(SongMeta.VoiceNames.Values);
        int voiceNameCount = voiceNames.Count;
        if (voiceNameCount > 1)
        {
            int voiceIndex = (SceneData.SelectedPlayerProfiles.IndexOf(playerProfile) % voiceNameCount);
            return voiceNames[voiceIndex];
        }
        else
        {
            return Voice.soloVoiceName;
        }
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            pauseOverlay.SetActive(true);
            songAudioPlayer.PauseAudio();
        }
        else
        {
            pauseOverlay.SetActive(false);
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
        bb.BindExistingInstance(playerUiArea);
        return bb.GetBindings();
    }
}
