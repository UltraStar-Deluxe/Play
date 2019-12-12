using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;

public class SongEditorSceneController : MonoBehaviour, IBinder
{
    public string defaultSongName;

    [TextArea(3, 8)]
    [Tooltip("Convenience text field to paste and copy song names when debugging.")]
    public string defaultSongNamePasteBin;

    public AudioWaveFormVisualizer audioWaveFormVisualizer;

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

        List<Voice> voices = VoiceIdToVoiceMap.Values.ToList();
        bb.Bind("voices").ToExistingInstance(voices);
        return bb.GetBindings();
    }

    void Start()
    {
        Debug.Log($"Start editing of '{SceneData.SelectedSongMeta.Title}' at {SceneData.PositionInSongMillis} milliseconds.");
    }

    void Update()
    {
        if (!audioWaveFormInitialized && audioClip != null && audioClip.samples > 0)
        {
            using (new DisposableStopwatch($"Created audio waveform in <millis> ms"))
            {
                audioWaveFormInitialized = true;
                audioWaveFormVisualizer.DrawWaveFormMinAndMaxValues(audioClip);
            }
        }
    }

    public void OnBackButtonClicked()
    {
        ContinueToSingScene();
    }

    private void ContinueToSingScene()
    {
        if (sceneData.PreviousSceneData is SingSceneData)
        {
            (sceneData.PreviousSceneData as SingSceneData).PositionInSongMillis = sceneData.PositionInSongMillis;
        }
        SceneNavigator.Instance.LoadScene(sceneData.PreviousScene, sceneData.PreviousSceneData);
    }

    private SongEditorSceneData CreateDefaultSceneData()
    {
        SongEditorSceneData defaultSceneData = new SongEditorSceneData();
        defaultSceneData.PreviousScene = EScene.SongSelectScene;
        defaultSceneData.PreviousSceneData = null;
        defaultSceneData.PositionInSongMillis = 0;
        defaultSceneData.SelectedSongMeta = SongMetaManager.Instance.FindSongMeta(defaultSongName);
        return defaultSceneData;
    }
}
