using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Mutes AudioSources in the scene to prevent audio stutter.
 */
public class MuteAudioSourcesBeforeSceneChangeControl : AbstractSingletonBehaviour, INeedInjection
{
    public static MuteAudioSourcesBeforeSceneChangeControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<MuteAudioSourcesBeforeSceneChangeControl>();
    
    [Inject]
    private SceneNavigator sceneNavigator;
    
    private readonly List<AudioSource> mutedAudioSources = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => OnBeforeSceneChange());
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());
    }

    private void OnSceneChanged()
    {
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
            () => UnmuteAudioSources()));
    }

    private void OnBeforeSceneChange()
    {
        MuteAudioSources();
    }
    
    private void MuteAudioSources()
    {
        Debug.Log("Muting AudioSources (before scene change)");
        mutedAudioSources.Clear();
        foreach (AudioSource audioSource in FindObjectsOfType<AudioSource>())
        {
            if (audioSource.GetComponentInParent<DontDestroyOnLoadManager>() == null
                && audioSource.clip != null)
            {
                Debug.Log($"Muting AudioSource: {audioSource.name} ({audioSource.clip.name}");
                audioSource.mute = true;
                mutedAudioSources.Add(audioSource);
            }
        }
    }
    
    private void UnmuteAudioSources()
    {
        Debug.Log("Unmuting AudioSources (after scene change)");
        foreach (AudioSource audioSource in mutedAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.mute = false;
            }
        }
        mutedAudioSources.Clear();
    }
}
