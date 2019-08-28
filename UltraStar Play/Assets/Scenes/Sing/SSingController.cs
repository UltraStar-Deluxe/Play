using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using NAudio.Wave;

public class SSingController : MonoBehaviour
{
    public string DefaultSongName;
    public string DefaultPlayerProfileName;

    public RectTransform PlayerUiArea;
    public RectTransform PlayerUiPrefab;

    public SongMeta SongMeta;
    public PlayerProfile PlayerProfile;

    public VideoPlayer VideoPlayer;

    private IWavePlayer m_WaveOutDevice;
    private WaveStream m_MainOutputStream;
    private WaveChannel32 m_VolumeStream;

    public double CurrentBeat {
        get {
            double millisInSong = m_MainOutputStream.CurrentTime.TotalMilliseconds;
            var result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
            if(result < 0) {
                result = 0;
            }
            return result;
        }
    }

    // The current position in the song in milliseconds.
    public double PositionInSongInMillis { 
        get {
            return m_MainOutputStream.CurrentTime.TotalMilliseconds;
        }
    }

    // The current position in the song in milliseconds.
    public double PositionInSongInSeconds { 
        get {
            return m_MainOutputStream.CurrentTime.TotalSeconds;
        }
    }

    void Start() {
        SongMeta = SceneDataBus.GetData(ESceneData.Song, GetDefaultSongMeta );
        PlayerProfile = SceneDataBus.GetData(ESceneData.PlayerProfile, GetDefaultPlayerProfile );

        VideoPlayer = FindObjectOfType<VideoPlayer>();

        Debug.Log($"{PlayerProfile.Name} starts singing of {SongMeta.Title}.");

        var songMetaPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Filename;
        var songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;

        LoadAudio(songPath);
        m_WaveOutDevice.Play();
        
        // Create player ui for each player (currently there is only one player)
        CreatePlayerUi();
        
        Invoke("StartVideoPlayback", SongMeta.VideoGap);
    }

    void OnDestroy()
    {
        UnloadAudio();
    }

    private void StartVideoPlayback() {
        var videoPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Video;
        if(File.Exists(videoPath)) {
            VideoPlayer.url = "file://" + videoPath;
            InvokeRepeating("SyncVideoWithMusic", 5f, 10f);
        } else {
            VideoPlayer.enabled = false;
            // TODO: Use cover as fallback
        }
    }

    private void SyncVideoWithMusic() {
        var secondsInSong = m_MainOutputStream.CurrentTime.TotalSeconds;
        if(VideoPlayer.length > secondsInSong) {
            VideoPlayer.time = secondsInSong;
        }
    }

    private void CreatePlayerUi()
    {
        // Remove old player ui
        foreach(RectTransform oldPlayerUi in PlayerUiArea) {
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
        var defaultPlayerProfiles = PlayerProfilesManager.PlayerProfiles.Where(it => it.Name == DefaultPlayerProfileName);
        if(defaultPlayerProfiles.Count() == 0) {
            throw new Exception("The default player profile was not found.");
        }
        return defaultPlayerProfiles.First();
    }

    private SongMeta GetDefaultSongMeta()
    {
        SongMetaManager.ScanFiles();
        var defaultSongMetas = SongMetaManager.GetSongMetas().Where(it => it.Title == DefaultSongName);
        if(defaultSongMetas.Count() == 0) {
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

    private void LoadAudio(string path) {
        var url = "file://"+path;
        WWW www = new WWW(url);
        while (!www.isDone) { };
        if (!string.IsNullOrEmpty(www.error)) {
            throw new Exception(www.error);
        }

        byte[] bytes = www.bytes;
        if (!LoadAudioFromData(bytes))
        {
            throw new Exception("Cannot open mp3 file!");
        }
        
        Resources.UnloadUnusedAssets();
    }

    private void UnloadAudio() {
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
