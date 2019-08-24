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

    private SongMeta m_songMeta;
    private PlayerProfile m_playerProfile;

    private VideoPlayer m_videoPlayer;

    private IWavePlayer mWaveOutDevice;
    private WaveStream mMainOutputStream;
    private WaveChannel32 mVolumeStream;

    public double CurrentBeat { get; internal set; }

    void Start() {
        m_songMeta = SceneDataBus.GetData(ESceneData.Song, GetDefaultSongMeta );
        m_playerProfile = SceneDataBus.GetData(ESceneData.PlayerProfile, GetDefaultPlayerProfile );

        m_videoPlayer = FindObjectOfType<VideoPlayer>();

        Debug.Log($"{m_playerProfile.Name} starts singing of {m_songMeta.Title}.");

        var songMetaPath = m_songMeta.Directory + Path.DirectorySeparatorChar + m_songMeta.Filename;
        var songPath = m_songMeta.Directory + Path.DirectorySeparatorChar + m_songMeta.Mp3;

        LoadAudio(songPath);
        mWaveOutDevice.Play();
        
        // Create player ui for each player (currently there is only one player)
        CreatePlayerUi();
        
        InvokeRepeating("UpdateCurrentBeat", m_songMeta.Gap / 1000.0f, 0.1f);

        Invoke("StartVideoPlayback", m_songMeta.VideoGap);
    }

    void OnDisable()
    {
        UnloadAudio();
    }

    void UpdateCurrentBeat() {
        // Map time in song to current beat.
        double millisInSong = mMainOutputStream.CurrentTime.TotalMilliseconds;
        var millisInSongAfterGap = millisInSong - m_songMeta.Gap;
        CurrentBeat = m_songMeta.Bpm * millisInSongAfterGap / 1000.0 / 60.0;
        if(CurrentBeat < 0) {
            CurrentBeat = 0;
        }
        // Debug.Log($"secondsInSongAfterGap: {millisInSongAfterGap / 1000.0}");
        // Debug.Log($"Current beat: {CurrentBeat}");
    }

    private void StartVideoPlayback() {
        var videoPath = m_songMeta.Directory + Path.DirectorySeparatorChar + m_songMeta.Video;
        if(File.Exists(videoPath)) {
            m_videoPlayer.url = "file://" + videoPath;
            InvokeRepeating("SyncVideoWithMusic", 0.5f, 0.5f);
        } else {
            m_videoPlayer.enabled = false;
            // TODO: Use cover as fallback
        }
    }

    private void SyncVideoWithMusic() {
        var secondsInSong = mMainOutputStream.CurrentTime.TotalSeconds;
        if(m_videoPlayer.length > secondsInSong) {
            m_videoPlayer.time = secondsInSong;
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

        // Load the voice for the SentenceDisplayer of the PlayerUi
        var sentenceDisplayer = playerUi.GetComponentInChildren<SentenceDisplayer>();
        sentenceDisplayer.LoadVoice(m_songMeta, null);
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
            mMainOutputStream = new Mp3FileReader(tmpStr);
            mVolumeStream = new WaveChannel32(mMainOutputStream);

            mWaveOutDevice = new WaveOut();
            mWaveOutDevice.Init(mVolumeStream);

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
        if (mWaveOutDevice != null)
        {
            mWaveOutDevice.Stop();
        }

        if (mMainOutputStream != null)
        {
            // this one really closes the file and ACM conversion
            mVolumeStream.Close();
            mVolumeStream = null;

            // this one does the metering stream
            mMainOutputStream.Close();
            mMainOutputStream = null;
        }
        if (mWaveOutDevice != null)
        {
            mWaveOutDevice.Dispose();
            mWaveOutDevice = null;
        }
    }
}
