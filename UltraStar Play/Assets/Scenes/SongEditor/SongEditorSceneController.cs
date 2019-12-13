using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;

public class SongEditorSceneController : MonoBehaviour, IBinder
{
    [InjectedInInspector]
    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public AudioWaveFormVisualizer audioWaveFormVisualizer;

    [InjectedInInspector]
    public PositionInSongIndicator positionInSongIndicator;

    private bool audioWaveFormInitialized;

    public SongMeta SongMeta
    {
        get
        {
            return SceneData.SelectedSongMeta;
        }
    }

    Dictionary<string, Voice> voiceIdToVoiceMap;

    private Dictionary<string, Voice> VoiceIdToVoiceMap
    {
        get
        {
            if (voiceIdToVoiceMap == null)
            {
                voiceIdToVoiceMap = SongMetaManager.GetVoices(SongMeta);
            }
            return voiceIdToVoiceMap;
        }
    }

    private AudioClip audioClip;
    public AudioClip AudioClip
    {
        get
        {
            if (audioClip == null && SongMeta != null)
            {
                string path = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;
                audioClip = AudioManager.GetAudioClip(path);
            }
            return audioClip;
        }
    }

    private SongEditorSceneData sceneData;
    public SongEditorSceneData SceneData
    {
        get
        {
            if (sceneData == null)
            {
                sceneData = SceneNavigator.Instance.GetSceneData<SongEditorSceneData>(CreateDefaultSceneData());
            }
            return sceneData;
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        // Note that the SceneData, SongMeta, and AudioClip are loaded on access here if not done yet.
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(AudioClip);
        bb.BindExistingInstance(this);

        List<Voice> voices = VoiceIdToVoiceMap.Values.ToList();
        bb.Bind("voices").ToExistingInstance(voices);
        return bb.GetBindings();
    }

    void Start()
    {
        Debug.Log($"Start editing of '{SceneData.SelectedSongMeta.Title}' at {SceneData.PositionInSongInMillis} ms.");
        songAudioPlayer.Init(SongMeta);
        songVideoPlayer.Init(SongMeta);

        SetPositionInSongInMillis(SceneData.PositionInSongInMillis);
    }

    void Update()
    {
        // Create the audio waveform image if not done yet.
        if (!audioWaveFormInitialized && audioClip != null && audioClip.samples > 0)
        {
            using (new DisposableStopwatch($"Created audio waveform in <millis> ms"))
            {
                audioWaveFormInitialized = true;
                audioWaveFormVisualizer.DrawWaveFormMinAndMaxValues(audioClip);
            }
        }

        // Synchronize music and video.
        if (songAudioPlayer.IsPlaying)
        {
            songVideoPlayer.SetPositionInSongInMillis(songAudioPlayer.PositionInSongInMillis);
            positionInSongIndicator.SetPositionInSongInPercent(songAudioPlayer.PositionInSongInPercent);
        }
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
            songVideoPlayer.PauseVideo();
        }
        else
        {
            songAudioPlayer.PlayAudio();
            songVideoPlayer.PlayVideo();
        }
    }

    public void SetPositionInSongInMillis(double millis)
    {
        songAudioPlayer.PositionInSongInMillis = millis;
        songVideoPlayer.SetPositionInSongInMillis(millis);
        songVideoPlayer.SyncVideoWithMusicImmediately(millis);
        if (songAudioPlayer.DurationOfSongInMillis > 0)
        {
            double percent = millis / songAudioPlayer.DurationOfSongInMillis;
            positionInSongIndicator.SetPositionInSongInPercent(percent);
        }
    }

    public void SetPositionInSongInPercent(double percent)
    {
        double newPositionInSongInMillis = songAudioPlayer.DurationOfSongInMillis * percent;
        SetPositionInSongInMillis(newPositionInSongInMillis);
    }

    public void OnBackButtonClicked()
    {
        ContinueToSingScene();
    }

    public void OnSaveButtonClicked()
    {
        SaveSong();
    }

    private void SaveSong()
    {
        // TODO: Implement saving the song file.
        // TODO: A backup of the original file should be created (copy original txt file, but only once),
        // to avoid breaking songs because of issues in loading / saving the song data.
        // (This project is still in early development and untested and should not break songs of the users.)
    }

    private void ContinueToSingScene()
    {
        if (sceneData.PreviousSceneData is SingSceneData)
        {
            SingSceneData singSceneData = sceneData.PreviousSceneData as SingSceneData;
            singSceneData.PositionInSongInMillis = songAudioPlayer.PositionInSongInMillis;
        }
        SceneNavigator.Instance.LoadScene(sceneData.PreviousScene, sceneData.PreviousSceneData);
    }

    private SongEditorSceneData CreateDefaultSceneData()
    {
        SongEditorSceneData defaultSceneData = new SongEditorSceneData();
        defaultSceneData.PositionInSongInMillis = 0;
        defaultSceneData.SelectedSongMeta = SongMetaManager.Instance.FindSongMeta(defaultSongName);

        // Set up PreviousSceneData to directly start the SingScene.
        defaultSceneData.PreviousScene = EScene.SingScene;

        SingSceneData singSceneData = new SingSceneData();

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
        playerProfiles.Add(playerProfile);
        singSceneData.SelectedPlayerProfiles = playerProfiles;

        defaultSceneData.PreviousSceneData = singSceneData;

        return defaultSceneData;
    }
}
