using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using NAudio.Wave;
using UnityEngine.Networking;
using System.Threading;

public class SingSceneController : MonoBehaviour
{
    public SingSceneData singSceneData;

    public string DefaultSongName;
    public string DefaultPlayerProfileName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public RectTransform PlayerUiArea;
    public RectTransform PlayerUiPrefab;

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

    private VideoPlayer VideoPlayer;

    private IWavePlayer m_WaveOutDevice;
    private WaveStream m_MainOutputStream;
    private WaveChannel32 m_VolumeStream;

    public double CurrentBeat
    {
        get
        {
            if (m_MainOutputStream == null)
            {
                return 0;
            }
            else
            {
                double millisInSong = m_MainOutputStream.CurrentTime.TotalMilliseconds;
                var result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
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
            if (m_MainOutputStream == null)
            {
                return 0;
            }
            else
            {
                return m_MainOutputStream.CurrentTime.TotalMilliseconds;
            }
        }
    }

    // The current position in the song in milliseconds.
    public double PositionInSongInSeconds
    {
        get
        {
            if (m_MainOutputStream == null)
            {
                return 0;
            }
            else
            {
                return m_MainOutputStream.CurrentTime.TotalSeconds;
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
        if (m_WaveOutDevice != null && m_WaveOutDevice.PlaybackState == PlaybackState.Playing)
        {
            Debug.Log("Song already playing");
            return;
        }

        VideoPlayer = FindObjectOfType<VideoPlayer>();

        Debug.Log($"{PlayerProfile.Name} starts singing of {SongMeta.Title}.");

        var songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;

        LoadAudio(songPath);
        m_WaveOutDevice.Play();
        if (singSceneData.PositionInSongMillis > 0)
        {
            m_MainOutputStream.CurrentTime = TimeSpan.FromMilliseconds(singSceneData.PositionInSongMillis);
        }

        // Create player ui for each player (currently there is only one player)
        CreatePlayerUi();

        // Start any associated video
        Invoke("StartVideoPlayback", SongMeta.VideoGap);

        // Go to next scene when the song finishes
        Invoke("CheckSongFinished", m_MainOutputStream.TotalTime.Seconds);
    }

    void OnDisable()
    {
        if (m_MainOutputStream != null)
        {
            singSceneData.PositionInSongMillis = m_MainOutputStream.CurrentTime.TotalMilliseconds;
            UnloadAudio();
        }
        if (VideoPlayer != null)
        {
            VideoPlayer.Stop();
        }
    }

    void OnEnable()
    {
        if (singSceneData.IsRestart)
        {
            singSceneData.IsRestart = false;
            singSceneData.PositionInSongMillis = 0;
        }

        if (m_MainOutputStream == null && singSceneData.PositionInSongMillis > 0)
        {
            Debug.Log("Reloading Song...");
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
        if (m_MainOutputStream == null)
        {
            return;
        }

        double totalMillis = m_MainOutputStream.TotalTime.TotalMilliseconds;
        double currentMillis = m_MainOutputStream.CurrentTime.TotalMilliseconds;
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
        var videoPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Video;
        if (File.Exists(videoPath))
        {
            VideoPlayer.url = "file://" + videoPath;
            InvokeRepeating("SyncVideoWithMusic", 5f, 10f);
        }
        else
        {
            VideoPlayer.enabled = false;
        }
    }

    private void SyncVideoWithMusic()
    {
        if (m_MainOutputStream == null)
        {
            return;
        }

        var secondsInSong = m_MainOutputStream.CurrentTime.TotalSeconds;
        if (VideoPlayer.length > secondsInSong)
        {
            VideoPlayer.time = secondsInSong;
        }
    }

    private void CreatePlayerUi()
    {
        // Remove old player ui
        foreach (RectTransform oldPlayerUi in PlayerUiArea)
        {
            GameObject.Destroy(oldPlayerUi.gameObject);
        }

        // Create new player ui for each player.
        var playerUi = GameObject.Instantiate(PlayerUiPrefab);
        playerUi.SetParent(PlayerUiArea);

        // Associate a LyricsDisplayer with the SentenceDisplayer
        var sentenceDisplayer = playerUi.GetComponentInChildren<SentenceDisplayer>();
        var lyricsDisplayer = FindObjectOfType<LyricsDisplayer>();
        sentenceDisplayer.LyricsDisplayer = lyricsDisplayer;

        // Load the voice for the SentenceDisplayer of the PlayerUi
        sentenceDisplayer.LoadVoice(SongMeta, null);
    }

    private PlayerProfile GetDefaultPlayerProfile()
    {
        var allPlayerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        var defaultPlayerProfiles = allPlayerProfiles.Where(it => it.Name == DefaultPlayerProfileName);
        if (defaultPlayerProfiles.Count() == 0)
        {
            throw new Exception("The default player profile was not found.");
        }
        return defaultPlayerProfiles.First();
    }

    private SongMeta GetDefaultSongMeta()
    {
        var defaultSongMetas = SongMetaManager.Instance.SongMetas.Where(it => it.Title == DefaultSongName);
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
            m_MainOutputStream = new Mp3FileReader(tmpStr);
            m_VolumeStream = new WaveChannel32(m_MainOutputStream);

            m_WaveOutDevice = new WaveOutEvent();
            m_WaveOutDevice.Init(m_VolumeStream);

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
        if (m_WaveOutDevice != null)
        {
            m_WaveOutDevice.Stop();
        }

        if (m_MainOutputStream != null)
        {
            // this one really closes the file and ACM conversion
            m_VolumeStream.Close();
            m_VolumeStream = null;

            // this one does the metering stream
            m_MainOutputStream.Close();
            m_MainOutputStream = null;
        }
        if (m_WaveOutDevice != null)
        {
            m_WaveOutDevice.Dispose();
            m_WaveOutDevice = null;
        }
    }

}
