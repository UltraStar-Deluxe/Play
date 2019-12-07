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
    public GameObject videoImageAndPlayerContainer;

    private VideoPlayer videoPlayer;
    public List<PlayerController> PlayerControllers { get; private set; } = new List<PlayerController>();

    public List<AbstractDummySinger> DummySingers { get; private set; } = new List<AbstractDummySinger>();

    private AudioSource audioPlayer;

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

    public double CurrentBeat
    {
        get
        {
            if (audioPlayer.clip == null)
            {
                return 0;
            }
            else
            {
                double millisInSong = PositionInSongInMillis;
                double result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
                if (result < 0)
                {
                    result = 0;
                }
                return result;
            }
        }
    }

    // The last frame in which the position in the song was calculated
    private int positionInSongInMillisFrame;

    // The current position in the song in milliseconds.
    private double positionInSongInMillis;
    public double PositionInSongInMillis
    {
        get
        {
            if (audioPlayer == null || audioPlayer.clip == null)
            {
                return 0;
            }
            // The samples of an AudioClip change concurrently,
            // even when they are queried in the same frame (e.g. Update() of different scripts).
            // For a given frame, the position in the song should be the same for all scripts,
            // which is why the value is only updated once per frame.
            if (positionInSongInMillisFrame != Time.frameCount)
            {
                positionInSongInMillisFrame = Time.frameCount;
                positionInSongInMillis = 1000.0f * (double)audioPlayer.timeSamples / (double)audioPlayer.clip.frequency;
            }
            return positionInSongInMillis;
        }

        set
        {
            if (audioPlayer.clip == null)
            {
                return;
            }
            int newTimeSamples = (int)((value / 1000.0) * audioPlayer.clip.frequency);
            audioPlayer.timeSamples = newTimeSamples;

            SyncVideoWithMusicImmediately();
        }
    }

    public double DurationOfSongInMillis
    {
        get
        {
            if (audioPlayer.clip == null)
            {
                return 0;
            }
            double lengthInMillis = 1000.0 * audioPlayer.clip.samples / audioPlayer.clip.frequency;
            return lengthInMillis;

        }
    }

    void Awake()
    {
        videoPlayer = FindObjectOfType<VideoPlayer>();
        audioPlayer = FindObjectOfType<AudioSource>();

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
        Debug.Log($"[{playerProfilesCsv}] start (or continue) singing of {SongMeta.Title}.");
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
            ShowBackgroundImage();
        }
        else
        {
            StartVideoPlayback();
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
            sceneData.PositionInSongMillis = 0;
        }
        else
        {
            sceneData.PositionInSongMillis = PositionInSongInMillis;
        }
        if (audioPlayer && audioPlayer.clip != null)
        {
            UnloadAudio();
        }

        if (videoPlayer != null && videoPlayer.gameObject.activeInHierarchy)
        {
            videoPlayer.Stop();
        }
    }

    void Update()
    {
        UpdateVideoStart();
        PlayerControllers.ForEach(it => it.SetCurrentBeat(CurrentBeat));

        // TODO: Updating the pitch detection (including the dummy singers) for this frame must come after updating the current sentence.
        // Otherwise, a pitch event may be fired for a beat of the "previous" sentence where no note is expected,
        // afterwards the sentence changes (the note is expected now), but the pitch event is lost.

        if (Application.isEditor)
        {
            DummySingers.ForEach(it => it.UpdateSinging(CurrentBeat));
        }
    }

    private void UpdateVideoStart()
    {
        // Negative VideoGap: Start video after a pause in seconds.
        if (SongMeta.VideoGap < 0
            && videoPlayer.gameObject.activeInHierarchy
            && videoPlayer.isPaused
            && (PositionInSongInMillis >= (-SongMeta.VideoGap * 1000)))
        {
            videoPlayer.Play();
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
            PositionInSongInMillis = targetPositionInMillis;
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
        songEditorSceneData.PositionInSongMillis = PositionInSongInMillis;
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

    private void StartVideoPlayback()
    {
        string videoPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Video;
        if (File.Exists(videoPath))
        {
            StartVideoPlayback(videoPath);
        }
        else
        {
            Debug.LogWarning("Video file '" + videoPath + "' does not exist. Showing background image instead.");
            ShowBackgroundImage();
        }
    }

    private void StartVideoPlayback(string videoPath)
    {
        videoPlayer.url = "file://" + videoPath;
        if (string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogWarning("Setting VideoPlayer URL failed. Showing background image instead.");
            ShowBackgroundImage();
        }
        else
        {
            if (SongMeta.VideoGap > 0)
            {
                // Positive VideoGap, thus skip the start of the video
                videoPlayer.time = SongMeta.VideoGap;
                videoPlayer.Play();
            }
            else if (SongMeta.VideoGap < 0)
            {
                // Negative VideoGap, thus wait a little before starting the video
                videoPlayer.Pause();
            }
            else
            {
                // No VideoGap, thus start the video immediately
                videoPlayer.Play();
            }
            InvokeRepeating("SyncVideoWithMusicSmoothly", 0.5f, 0.5f);
        }
    }

    private void ShowBackgroundImage()
    {
        videoImageAndPlayerContainer.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(SongMeta.Background))
        {
            ShowCoverImageAsBackground();
        }
        else
        {
            string backgroundImagePath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Background;
            if (File.Exists(backgroundImagePath))
            {
                Sprite backgroundImageSprite = ImageManager.LoadSprite(backgroundImagePath);
                backgroundImage.sprite = backgroundImageSprite;
            }
            else
            {
                Debug.LogWarning("Background image '" + backgroundImagePath + "'does not exist. Showing cover instead.");
                ShowCoverImageAsBackground();
            }
        }
    }

    private void ShowCoverImageAsBackground()
    {
        if (string.IsNullOrEmpty(SongMeta.Cover))
        {
            return;
        }

        string coverImagePath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Cover;
        if (File.Exists(coverImagePath))
        {
            Sprite coverImageSprite = ImageManager.LoadSprite(coverImagePath);
            backgroundImage.sprite = coverImageSprite;
        }
        else
        {
            Debug.LogWarning("Cover image '" + coverImagePath + "'does not exist.");
        }
    }

    private void SyncVideoWithMusicSmoothly()
    {
        if (audioPlayer.clip == null || !videoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        if (SongMeta.VideoGap < 0 && PositionInSongInMillis < (-SongMeta.VideoGap * 1000))
        {
            // Still waiting for the start of the video
            return;
        }

        double positionInVideoInSeconds = SongMeta.VideoGap + PositionInSongInMillis / 1000;
        double timeDifferenceInSeconds = positionInVideoInSeconds - videoPlayer.time;
        // Smooth out the time difference over a duration of 2 seconds
        float playbackSpeed = 1 + (float)(timeDifferenceInSeconds / 2.0);
        videoPlayer.playbackSpeed = playbackSpeed;
    }

    private void SyncVideoWithMusicImmediately()
    {
        if (audioPlayer.clip == null || !videoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        if (SongMeta.VideoGap < 0 && PositionInSongInMillis < (-SongMeta.VideoGap * 1000))
        {
            // Still waiting for the start of the video
            return;
        }

        double targetPositionInVideoInSeconds = SongMeta.VideoGap + PositionInSongInMillis / 1000;
        videoPlayer.time = targetPositionInVideoInSeconds;
        videoPlayer.playbackSpeed = 1f;
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

    public void TogglePauseSinging()
    {
        if (audioPlayer.clip == null)
        {
            return;
        }
        if (audioPlayer.isPlaying)
        {
            pauseOverlay.SetActive(true);
            audioPlayer.Pause();
            if (videoPlayer != null && videoPlayer.enabled)
            {
                videoPlayer.Pause();
            }
        }
        else
        {
            pauseOverlay.SetActive(false);
            audioPlayer.UnPause();
            if (videoPlayer != null && videoPlayer.enabled)
            {
                videoPlayer.Play();
            }
        }
    }

    private IEnumerator StartAudioPlayback()
    {
        if (audioPlayer.clip != null && audioPlayer.isPlaying)
        {
            Debug.LogWarning("Song already playing");
            yield break;
        }

        string songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;
        yield return LoadAudio(songPath);
        audioPlayer.Play();
        if (sceneData.PositionInSongMillis > 0)
        {
            Debug.Log($"Skipping forward to {sceneData.PositionInSongMillis} milliseconds");
            PositionInSongInMillis = sceneData.PositionInSongMillis;
        }
    }

    private IEnumerator LoadAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError("Error Loading Audio: " + path);
                Debug.LogError(www.error);
                SceneNavigator.Instance.LoadScene(EScene.SongSelectScene);
                yield break;
            }
            else
            {
                audioPlayer.clip = DownloadHandlerAudioClip.GetContent(www);

                // The time bar needs the duration of the song to calculate positions.
                // The duration of the song should be available now.
                InitTimeBar();
            }
        }

    }

    private void UnloadAudio()
    {
        if (audioPlayer.clip != null)
        {
            audioPlayer.Stop();
            audioPlayer.clip.UnloadAudioData();
            audioPlayer.clip = null;
        }
    }

}
