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
    public SingSceneData singSceneData;

    public string defaultSongName;
    public string defaultPlayerProfileName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public RectTransform playerUiArea;
    public RectTransform playerUiPrefab;

    public SongMeta SongMeta
    {
        get
        {
            return singSceneData.SelectedSongMeta;
        }
    }

    public PlayerProfile PlayerProfile
    {
        get
        {
            return singSceneData.SelectedPlayerProfiles[0];
        }
    }

    private VideoPlayer videoPlayer;

    private IWavePlayer waveOutDevice;
    private WaveStream mainOutputStream;
    private WaveChannel32 volumeStream;

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
                double millisInSong = mainOutputStream.CurrentTime.TotalMilliseconds;
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
                return mainOutputStream.CurrentTime.TotalMilliseconds;
            }
        }
    }

    // The current position in the song in milliseconds.
    public double PositionInSongInSeconds
    {
        get
        {
            if (mainOutputStream == null)
            {
                return 0;
            }
            else
            {
                return mainOutputStream.CurrentTime.TotalSeconds;
            }
        }
    }

    void Awake()
    {
        // Load scene data from static reference, if any
        singSceneData = SceneNavigator.Instance.GetSceneData(singSceneData);

        // Fill scene data with default values
        if (singSceneData.SelectedSongMeta == null)
        {
            singSceneData.SelectedSongMeta = GetDefaultSongMeta();
        }

        if (singSceneData.SelectedPlayerProfiles == null || singSceneData.SelectedPlayerProfiles.Count == 0)
        {
            singSceneData.AddPlayerProfile(GetDefaultPlayerProfile());
        }
    }

    void Start()
    {
        if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
        {
            Debug.Log("Song already playing");
            return;
        }

        videoPlayer = FindObjectOfType<VideoPlayer>();

        Debug.Log($"{PlayerProfile.Name} starts singing of {SongMeta.Title}.");

        string songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;

        LoadAudio(songPath);
        waveOutDevice.Play();
        if (singSceneData.PositionInSongMillis > 0)
        {
            mainOutputStream.CurrentTime = TimeSpan.FromMilliseconds(singSceneData.PositionInSongMillis);
        }

        // Create player ui for each player (currently there is only one player)
        CreatePlayerUi();

        // Start any associated video
        Invoke("StartVideoPlayback", SongMeta.VideoGap);

        // Go to next scene when the song finishes
        Invoke("CheckSongFinished", mainOutputStream.TotalTime.Seconds);
    }

    void OnDisable()
    {
        if (mainOutputStream != null)
        {
            singSceneData.PositionInSongMillis = mainOutputStream.CurrentTime.TotalMilliseconds;
            UnloadAudio();
        }
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    void OnEnable()
    {
        if (singSceneData.IsRestart)
        {
            singSceneData.IsRestart = false;
            singSceneData.PositionInSongMillis = 0;
        }

        if (mainOutputStream == null && singSceneData.PositionInSongMillis > 0)
        {
            Debug.Log("Reloading Song..");
            Start();
        }
    }

    public void Restart()
    {
        singSceneData.IsRestart = true;
        SceneNavigator.Instance.LoadScene(EScene.SingScene, singSceneData);
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

    private void FinishScene()
    {
        SceneNavigator.Instance.LoadScene(EScene.SongSelectScene);
    }

    private void StartVideoPlayback()
    {
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
        if (mainOutputStream == null)
        {
            return;
        }

        double secondsInSong = mainOutputStream.CurrentTime.TotalSeconds;
        if (videoPlayer.length > secondsInSong)
        {
            videoPlayer.time = secondsInSong;
        }
    }

    private void CreatePlayerUi()
    {
        // Remove old player ui
        foreach (RectTransform oldPlayerUi in playerUiArea)
        {
            GameObject.Destroy(oldPlayerUi.gameObject);
        }

        // Create new player ui for each player.
        RectTransform playerUi = GameObject.Instantiate(playerUiPrefab);
        playerUi.SetParent(playerUiArea);

        // Associate a LyricsDisplayer with the SentenceDisplayer
        SentenceDisplayer sentenceDisplayer = playerUi.GetComponentInChildren<SentenceDisplayer>();
        LyricsDisplayer lyricsDisplayer = FindObjectOfType<LyricsDisplayer>();
        sentenceDisplayer.LyricsDisplayer = lyricsDisplayer;

        // Load the voice for the SentenceDisplayer of the PlayerUi
        sentenceDisplayer.LoadVoice(SongMeta, null);
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
