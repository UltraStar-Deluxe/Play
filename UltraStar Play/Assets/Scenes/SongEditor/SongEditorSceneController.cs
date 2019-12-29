using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

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
    public SongEditorNoteRecorder songEditorNoteRecorder;

    [InjectedInInspector]
    public SongEditorSelectionController selectionController;

    [InjectedInInspector]
    public AudioWaveFormVisualizer audioWaveFormVisualizer;

    [InjectedInInspector]
    public NoteArea noteArea;

    [InjectedInInspector]
    public NoteAreaDragHandler noteAreaDragHandler;

    [InjectedInInspector]
    public MicrophonePitchTracker microphonePitchTracker;

    [InjectedInInspector]
    public Canvas canvas;

    [InjectedInInspector]
    public GraphicRaycaster graphicRaycaster;

    private readonly SongEditorLayerManager songEditorLayerManager = new SongEditorLayerManager();

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
        // Note that the SceneData and SongMeta are loaded on access here if not done yet.
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(SongMeta);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        bb.BindExistingInstance(noteArea);
        bb.BindExistingInstance(noteAreaDragHandler);
        bb.BindExistingInstance(songEditorLayerManager);
        bb.BindExistingInstance(microphonePitchTracker);
        bb.BindExistingInstance(songEditorNoteRecorder);
        bb.BindExistingInstance(selectionController);
        bb.BindExistingInstance(canvas);
        bb.BindExistingInstance(graphicRaycaster);
        bb.BindExistingInstance(this);

        List<Voice> voices = VoiceIdToVoiceMap.Values.ToList();
        bb.Bind("voices").ToExistingInstance(voices);
        return bb.GetBindings();
    }

    void Awake()
    {
        Debug.Log($"Start editing of '{SceneData.SelectedSongMeta.Title}' at {SceneData.PositionInSongInMillis} ms.");
        songAudioPlayer.Init(SongMeta);
        songVideoPlayer.Init(SongMeta, songAudioPlayer);

        songAudioPlayer.PositionInSongInMillis = SceneData.PositionInSongInMillis;
    }

    void Update()
    {
        // Create the audio waveform image if not done yet.
        if (!audioWaveFormInitialized && songAudioPlayer.HasAudioClip && songAudioPlayer.AudioClip.samples > 0)
        {
            using (new DisposableStopwatch($"Created audio waveform in <millis> ms"))
            {
                audioWaveFormInitialized = true;
                audioWaveFormVisualizer.DrawWaveFormMinAndMaxValues(songAudioPlayer.AudioClip);
            }
        }
    }

    public void TogglePlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
        }
        else
        {
            songAudioPlayer.PlayAudio();
        }
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
