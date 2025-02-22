using System;
using UniInject;
using UnityEngine;

public class AutoVocalsIsolationSceneMod : ISceneMod
{
    [Inject]
    private AudioSeparationManager audioSeparationManager;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        try {
            if (sceneEnteredContext.Scene == EScene.SingScene) {
                SingSceneControl singSceneControl = GameObjectUtils.FindObjectOfType<SingSceneControl>(false);
                SongMeta songMeta = singSceneControl.SongMeta;
                StartVocalsIsolation(songMeta);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to auto start vocals isolation: " + e.Message);
        }
    }

    private async void StartVocalsIsolation(SongMeta songMeta)
    {
        if (SongMetaUtils.VocalsAudioResourceExists(songMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
        {
            return;
        }

        await audioSeparationManager.ProcessSongMetaJob(songMeta, true).RunAsync();
    }
}

public class AutoVocalsIsolationMonoBehaviour : MonoBehaviour, INeedInjection
{
    // Awake is called once after instantiation
    private void Awake()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationMonoBehaviour)}.Awake");
    }

    // Start is called once before Update
    private void Start()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationMonoBehaviour)}.Start");
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void OnDestroy()
    {
        Debug.Log($"{nameof(AutoVocalsIsolationMonoBehaviour)}.OnDestroy");
        // GameObjects are destroyed before the next scene is loaded.
        // To persist a GameObject across scene changes, make it a child of DontDestroyOnLoadManager.
    }
}