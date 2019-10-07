using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using NAudio.Wave;

public class SingSceneController : MonoBehaviour
{
    // Constant delay when querying the position in the song.
    // Its source could be that calculating the position in the song takes some time for itself.
    public double positionInSongDelayInMillis = 150;

    public SingSceneData sceneData;

    public string defaultSongName;
    public string defaultPlayerProfileName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public PlayerController playerControllerPrefab;

    private VideoPlayer videoPlayer;
    public List<PlayerController> PlayerControllers { get; private set; } = new List<PlayerController>();

    private IWavePlayer waveOutDevice;
    private WaveStream mainOutputStream;
    private WaveChannel32 volumeStream;

    private double timeOfLastMeasuredPositionInSongInMillis;
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
                    timeOfLastMeasuredPositionInSongInMillis = 0;
                    return posInMillis;
                }
                else
                {
                    // No new measurement from the audio lib.
                    // Improve approximation by adding the time since the last new measurement.
                    return posInMillis + timeOfLastMeasuredPositionInSongInMillis;
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
        // Load scene data from static reference, if any
        sceneData = SceneNavigator.Instance.GetSceneData(sceneData);

        // Fill scene data with default values
        if (sceneData.SelectedSongMeta == null)
        {
            sceneData.SelectedSongMeta = GetDefaultSongMeta();
        }

        if (sceneData.SelectedPlayerProfiles == null || sceneData.SelectedPlayerProfiles.Count == 0)
        {
            sceneData.AddPlayerProfile(GetDefaultPlayerProfile());
        }
    }

    void OnEnable()
    {
        string playerProfilesCsv = String.Join(",", sceneData.SelectedPlayerProfiles.Select(it => it.Name));
        Debug.Log($"[{playerProfilesCsv}] start (or continue) singing of {SongMeta.Title}.");

        // Start the music
        StartAudioPlayback();

        // Start any associated video
        Invoke("StartVideoPlayback", SongMeta.VideoGap);

        // Go to next scene when the song finishes
        Invoke("CheckSongFinished", mainOutputStream.TotalTime.Seconds);

        // Handle players        
        foreach (PlayerProfile playerProfile in sceneData.SelectedPlayerProfiles)
        {
            CreatePlayerController(playerProfile);
        }

        // Associate LyricsDisplayer with one of the (duett) players
        LyricsDisplayer lyricsDisplayer = FindObjectOfType<LyricsDisplayer>();
        PlayerControllers[0].LyricsDisplayer = lyricsDisplayer;
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

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    void Update()
    {
        timeOfLastMeasuredPositionInSongInMillis += Time.deltaTime * 1000.0f;
        PlayerControllers.ForEach(it => it.SetPositionInSongInMillis(PositionInSongInMillis));
    }

    public void SkipToNextSentence()
    {
        double nextStartBeat = PlayerControllers.Select(it => it.GetNextStartBeat()).Min();
        if (nextStartBeat == double.MaxValue)
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
        videoPlayer = FindObjectOfType<VideoPlayer>();
        string videoPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Video;
        if (File.Exists(videoPath))
        {
            videoPlayer.url = "file://" + videoPath;
            InvokeRepeating("SyncVideoWithMusic", 5f, 10f);
        }
        else
        {
            videoPlayer.enabled = false;
        }
    }

    private void SyncVideoWithMusic()
    {
        if (mainOutputStream == null || videoPlayer == null)
        {
            return;
        }

        double secondsInSong = mainOutputStream.CurrentTime.TotalSeconds;
        if (videoPlayer.length > secondsInSong)
        {
            videoPlayer.time = secondsInSong;
        }
    }

    private void CreatePlayerController(PlayerProfile playerProfile)
    {
        PlayerControllers.Clear();

        string voiceIdentifier = GetVoiceIdentifier(playerProfile);
        PlayerController playerController = GameObject.Instantiate<PlayerController>(playerControllerPrefab);
        playerController.Init(sceneData.SelectedSongMeta, playerProfile, voiceIdentifier);

        PlayerControllers.Add(playerController);
    }

    private string GetVoiceIdentifier(PlayerProfile playerProfile)
    {
        bool isDuett = false;
        if (isDuett && sceneData.SelectedPlayerProfiles.Count == 2)
        {
            int voiceIndex = sceneData.SelectedPlayerProfiles.IndexOf(playerProfile) % 2;
            return "P" + voiceIndex;
        }
        else
        {
            return null;
        }
    }

    private PlayerProfile GetDefaultPlayerProfile()
    {
        List<PlayerProfile> allPlayerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        IEnumerable<PlayerProfile> defaultPlayerProfiles = allPlayerProfiles.Where(it => it.Name == defaultPlayerProfileName);
        if (defaultPlayerProfiles.Count() == 0)
        {
            throw new Exception("The default player profile was not found.");
        }
        return defaultPlayerProfiles.First();
    }

    private SongMeta GetDefaultSongMeta()
    {
        IEnumerable<SongMeta> defaultSongMetas = SongMetaManager.Instance.SongMetas.Where(it => it.Title == defaultSongName);
        if (defaultSongMetas.Count() == 0)
        {
            throw new Exception("The default song was not found.");
        }
        return defaultSongMetas.First();
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

    private bool LoadAudioFromData(byte[] data)
    {
        try
        {
            MemoryStream tmpStr = new MemoryStream(data);
            mainOutputStream = new Mp3FileReader(tmpStr);
            volumeStream = new WaveChannel32(mainOutputStream);

            waveOutDevice = new WaveOutEvent();
            waveOutDevice.Init(volumeStream);

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error Loading Audio: " + ex.Message);
        }

        return false;
    }

    private void LoadAudio(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes == null)
        {
            throw new Exception("Loading the mp3 data failed.");
        }

        if (!LoadAudioFromData(bytes))
        {
            throw new Exception("Loading the audio from the mp3 data failed.");
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
