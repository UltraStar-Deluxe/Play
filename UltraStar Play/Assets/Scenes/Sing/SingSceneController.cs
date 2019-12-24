using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading;
using NLayer;
using UniInject;

public class SingSceneController : MonoBehaviour, IOnHotSwapFinishedListener
{
    // Constant delay when querying the position in the song.
    // Its source could be that calculating the position in the song takes some time for itself.
    // After a change of the audio library this may or may not be needed anymore.
    // public double positionInSongDelayInMillis = 150;

    public SingSceneData sceneData;

    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public GameObject pauseOverlay;

    public PlayerController playerControllerPrefab;

    public Image backgroundImage;

    public List<PlayerController> PlayerControllers { get; private set; } = new List<PlayerController>();

    public List<AbstractDummySinger> DummySingers { get; private set; } = new List<AbstractDummySinger>();

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

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

    public SongMeta SongMeta
    {
        get
        {
            return sceneData.SelectedSongMeta;
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

    private void LoadSceneData()
    {
        // Load scene data from static reference, if any
        sceneData = SceneNavigator.Instance.GetSceneData(sceneData);

        // Fill scene data with default values
        if (sceneData.SelectedSongMeta == null)
        {
            sceneData.SelectedSongMeta = GetDefaultSongMeta();
        }

        if (sceneData.SelectedPlayerProfiles.IsNullOrEmpty())
        {
            PlayerProfile playerProfile = GetDefaultPlayerProfile();
            sceneData.SelectedPlayerProfiles.Add(playerProfile);
            sceneData.PlayerProfileToMicProfileMap[playerProfile] = GetDefaultMicProfile();
        }

        string playerProfilesCsv = string.Join(",", sceneData.SelectedPlayerProfiles.Select(it => it.Name));
        Debug.Log($"[{playerProfilesCsv}] start (or continue) singing of {SongMeta.Title} at {sceneData.PositionInSongInMillis} ms.");
    }

    void Start()
    {
        LoadSceneData();

        // Handle players
        List<PlayerProfile> playerProfilesWithoutMic = new List<PlayerProfile>();
        foreach (PlayerProfile playerProfile in sceneData.SelectedPlayerProfiles)
        {
            sceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            if (micProfile == null)
            {
                playerProfilesWithoutMic.Add(playerProfile);
            }
            CreatePlayerController(playerProfile, micProfile);
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
            PlayerControllers[0].LyricsDisplayer = lyricsDisplayer;
        }

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
        PlayerControllers.ForEach(it => it.SetCurrentBeat(CurrentBeat));

        // TODO: Updating the pitch detection (including the dummy singers) for this frame must come after updating the current sentence.
        // Otherwise, a pitch event may be fired for a beat of the "previous" sentence where no note is expected,
        // afterwards the sentence changes (the note is expected now), but the pitch event is lost.

        if (Application.isEditor)
        {
            DummySingers.ForEach(it => it.UpdateSinging(CurrentBeat));
        }
    }

    public MicProfile GetMicProfile(PlayerProfile playerProfile)
    {
        sceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
        return micProfile;
    }

    public void SkipToNextSentence()
    {
        double nextStartBeat = PlayerControllers.Select(it => it.GetNextStartBeat()).Min();
        if (nextStartBeat < 0)
        {
            return;
        }

        double targetPositionInMillis = BpmUtils.BeatToMillisecondsInSong(SongMeta, nextStartBeat) - 500;
        if (targetPositionInMillis > 0 && targetPositionInMillis > PositionInSongInMillis)
        {
            songAudioPlayer.PositionInSongInMillis = targetPositionInMillis;
        }
    }

    public void Restart()
    {
        sceneData.IsRestart = true;
        SceneNavigator.Instance.LoadScene(EScene.SingScene, sceneData);
    }

    public void OnOpenInEditorClicked()
    {
        SongEditorSceneData songEditorSceneData = new SongEditorSceneData();
        songEditorSceneData.PreviousSceneData = sceneData;
        songEditorSceneData.PreviousScene = EScene.SingScene;
        songEditorSceneData.PositionInSongInMillis = PositionInSongInMillis;
        songEditorSceneData.SelectedSongMeta = SongMeta;
        SceneNavigator.Instance.LoadScene(EScene.SongEditorScene, songEditorSceneData);
    }

    public void FinishScene()
    {
        // Open the singing results scene.
        SingingResultsSceneData singingResultsSceneData = new SingingResultsSceneData();
        singingResultsSceneData.SongMeta = SongMeta;
        foreach (PlayerController playerController in PlayerControllers)
        {
            SingingResultsSceneData.PlayerScoreData scoreData = new SingingResultsSceneData.PlayerScoreData();
            scoreData.TotalScore = playerController.PlayerScoreController.TotalScore;
            scoreData.GoldenNotesScore = playerController.PlayerScoreController.GoldenNotesTotalScore;
            scoreData.NormalNotesScore = playerController.PlayerScoreController.NormalNotesTotalScore;
            scoreData.PerfectSentenceBonusScore = playerController.PlayerScoreController.PerfectSentenceBonusTotalScore;
            singingResultsSceneData.AddPlayerScores(playerController.PlayerProfile, scoreData);
        }
        SceneNavigator.Instance.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private void CreatePlayerController(PlayerProfile playerProfile, MicProfile micProfile)
    {
        string voiceIdentifier = GetVoiceIdentifier(playerProfile);
        PlayerController playerController = GameObject.Instantiate<PlayerController>(playerControllerPrefab);
        playerController.Init(sceneData.SelectedSongMeta, playerProfile, voiceIdentifier, micProfile);

        PlayerControllers.Add(playerController);
    }

    private string GetVoiceIdentifier(PlayerProfile playerProfile)
    {
        bool isDuett = SongMeta.VoiceNames.Count > 1;
        if (isDuett && sceneData.SelectedPlayerProfiles.Count == 2)
        {
            int voiceIndex = (sceneData.SelectedPlayerProfiles.IndexOf(playerProfile) % 2) + 1;
            return "P" + voiceIndex;
        }
        else
        {
            return null;
        }
    }

    private PlayerProfile GetDefaultPlayerProfile()
    {
        List<PlayerProfile> allPlayerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;
        if (allPlayerProfiles.IsNullOrEmpty())
        {
            throw new UnityException("No player profiles found.");
        }
        PlayerProfile result = allPlayerProfiles[0];
        return result;
    }

    private MicProfile GetDefaultMicProfile()
    {
        return SettingsManager.Instance.Settings.MicProfiles.Where(it => it.IsEnabled && it.IsConnected).FirstOrDefault();
    }

    private SongMeta GetDefaultSongMeta()
    {
        IEnumerable<SongMeta> defaultSongMetas = SongMetaManager.Instance.SongMetas.Where(it => it.Title == defaultSongName);
        if (defaultSongMetas.Count() == 0)
        {
            throw new UnityException("The default song was not found.");
        }
        return defaultSongMetas.First();
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
        if (sceneData.PositionInSongInMillis > 0)
        {
            Debug.Log($"Skipping forward to {sceneData.PositionInSongInMillis} milliseconds");
            songAudioPlayer.PositionInSongInMillis = sceneData.PositionInSongInMillis;
        }
    }
}
