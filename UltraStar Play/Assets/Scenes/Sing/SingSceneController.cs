using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using NAudio.Wave;
using UnityEngine.UI;

public class SingSceneController : MonoBehaviour, IOnHotSwapFinishedListener
{
    // Constant delay when querying the position in the song.
    // Its source could be that calculating the position in the song takes some time for itself.
    public double positionInSongDelayInMillis = 150;

    public SingSceneData sceneData;

    [Range(0, 1)]
    public float volume = 1;

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

    private IWavePlayer waveOutDevice;
    private WaveStream mainOutputStream;
    private WaveChannel32 volumeStream;

    private double timeSinceLastMeasuredPositionInSongInMillis;
    private double lastMeasuredPositionInSongInMillis;

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
            if (mainOutputStream == null)
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

    // The current position in the song in milliseconds.
    public double PositionInSongInMillis
    {
        get
        {
            if (mainOutputStream == null)
            {
                return 0;
            }
            else
            {
                // The resolution of the NAudio time measurement is about 150 ms.
                // For the time between these measurements,
                // we improve the approximation by counting time using Unity's Time.deltaTime.
                double posInMillis = mainOutputStream.CurrentTime.TotalMilliseconds;
                // Reduce by constant offset. Could be that the measurement takes time itself ?
                posInMillis -= positionInSongDelayInMillis;
                if (posInMillis != lastMeasuredPositionInSongInMillis)
                {
                    // Got new measurement from the audio lib. This is (relatively) accurate.
                    lastMeasuredPositionInSongInMillis = posInMillis;
                    timeSinceLastMeasuredPositionInSongInMillis = 0;
                    return posInMillis;
                }
                else
                {
                    // No new measurement from the audio lib.
                    // Improve approximation by adding the time since the last new measurement.
                    return posInMillis + timeSinceLastMeasuredPositionInSongInMillis;
                }
            }
        }

        set
        {
            if (mainOutputStream == null)
            {
                return;
            }
            mainOutputStream.CurrentTime = TimeSpan.FromMilliseconds(value);
            SyncVideoWithMusic();
        }
    }

    public double DurationOfSongInMillis
    {
        get
        {
            if (mainOutputStream == null)
            {
                return 0;
            }

            return mainOutputStream.TotalTime.TotalMilliseconds;
        }
    }

    void Awake()
    {
        videoPlayer = FindObjectOfType<VideoPlayer>();

        if (!Application.isEditor && volume < 1)
        {
            volume = 1;
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

        StartMusicAndVideo();
    }

    private void StartMusicAndVideo()
    {
        // Start the music 
        StartAudioPlayback();

        // Start any associated video
        if (string.IsNullOrEmpty(SongMeta.Video))
        {
            ShowBackgroundImage();
        }
        else
        {
            StartVideoPlayback();
        }

        // Go to next scene when the song finishes
        Invoke("CheckSongFinished", mainOutputStream.TotalTime.Seconds);
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

        if (mainOutputStream != null)
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
        UpdateMusic();
        UpdateVideoStart();
        PlayerControllers.ForEach(it => it.SetPositionInSongInMillis(PositionInSongInMillis));
    }

    private void UpdateMusic()
    {
        if (waveOutDevice == null)
        {
            return;
        }

        if (waveOutDevice.PlaybackState == PlaybackState.Playing)
        {
            timeSinceLastMeasuredPositionInSongInMillis += Time.deltaTime * 1000.0f;
        }
        if (Application.isEditor)
        {
            volumeStream.Volume = volume;
        }
    }

    private void UpdateVideoStart()
    {
        // Negative VideoGap: Start video after a pause in seconds.
        if (SongMeta.VideoGap < 0 && videoPlayer.gameObject.activeInHierarchy && videoPlayer.isPaused)
        {
            if (PositionInSongInMillis >= (-SongMeta.VideoGap * 1000))
            {
                videoPlayer.Play();
            }
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

    private void CheckSongFinished()
    {
        if (mainOutputStream == null)
        {
            return;
        }

        double totalMillis = mainOutputStream.TotalTime.TotalMilliseconds;
        double currentMillis = mainOutputStream.CurrentTime.TotalMilliseconds;
        double missingMillis = totalMillis - currentMillis;
        if (missingMillis <= 0)
        {
            Invoke("FinishScene", 1f);
        }
        else
        {
            Invoke("CheckSongFinished", (float)(missingMillis / 1000.0));
        }
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
            InvokeRepeating("SyncVideoWithMusic", 5f, 10f);
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

    private void SyncVideoWithMusic()
    {
        if (mainOutputStream == null || !videoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        if (SongMeta.VideoGap < 0 && PositionInSongInMillis < (-SongMeta.VideoGap * 1000))
        {
            // Still waiting for the start of the video
            return;
        }

        float positionInVideoInSeconds = (float)(SongMeta.VideoGap + PositionInSongInMillis / 1000);
        if (videoPlayer.length > positionInVideoInSeconds)
        {
            videoPlayer.time = positionInVideoInSeconds;
        }
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
        if (waveOutDevice.PlaybackState == PlaybackState.Playing)
        {
            pauseOverlay.SetActive(true);
            waveOutDevice.Pause();
            if (videoPlayer != null && videoPlayer.enabled)
            {
                videoPlayer.Pause();
            }
        }
        else if (waveOutDevice.PlaybackState == PlaybackState.Paused)
        {
            pauseOverlay.SetActive(false);
            waveOutDevice.Play();
            if (videoPlayer != null && videoPlayer.enabled)
            {
                videoPlayer.Play();
            }
        }
    }

    private void StartAudioPlayback()
    {
        if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
        {
            Debug.LogWarning("Song already playing");
            return;
        }

        string songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;
        LoadAudio(songPath);
        waveOutDevice.Play();
        if (sceneData.PositionInSongMillis > 0)
        {
            Debug.Log($"Skipping forward to {sceneData.PositionInSongMillis} milliseconds");
            PositionInSongInMillis = sceneData.PositionInSongMillis;
        }
    }

    private void LoadAudio(string path)
    {
        try
        {
            mainOutputStream = new AudioFileReader(path);
            volumeStream = new WaveChannel32(mainOutputStream);
            volumeStream.Volume = volume;

            waveOutDevice = new WaveOutEvent();
            waveOutDevice.Init(volumeStream);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error Loading Audio: " + ex.Message);
            SceneNavigator.Instance.LoadScene(EScene.SongSelectScene);
            return;
        }

        Resources.UnloadUnusedAssets();
    }

    private void UnloadAudio()
    {
        if (waveOutDevice != null)
        {
            waveOutDevice.Stop();
        }

        if (mainOutputStream != null)
        {
            // this one really closes the file and ACM conversion
            volumeStream.Close();
            volumeStream = null;

            // this one does the metering stream
            mainOutputStream.Close();
            mainOutputStream = null;
        }
        if (waveOutDevice != null)
        {
            waveOutDevice.Dispose();
            waveOutDevice = null;
        }
    }

}
