using System;
using Eitan.Sherpa.Onnx.Unity.Mono.Components;
using UniInject;
using UnityEngine;

public class SourceSeparationDemo : MonoBehaviour
{
    [InjectedInInspector]
    public SourceSeparationComponent sourceSeparationComponent;

    private AudioClip audioClip;

    async void Start()
    {
        sourceSeparationComponent.SeparationReadyEvent.AddListener(OnSeparationReady);
        sourceSeparationComponent.ErrorEvent.AddListener(OnError);
        sourceSeparationComponent.InitializationStateChangedEvent.AddListener(OnInitializationStateChangedEvent);

        string filePath = "C:/Users/andre/Downloads/Sally's Song - Amy lee - short.mp3";
        audioClip = await AudioManager.LoadAudioClipFromUriAsync(filePath, false);
        if (audioClip == null)
        {
            Debug.LogError("Failed to load AudioClip");
            return;
        }

        Debug.Log("Loading source separation module");
        sourceSeparationComponent.TryLoadModule();
    }

    private void OnInitializationStateChangedEvent(bool ready)
    {
        Debug.Log("Source separation state changed: " + ready);
        if (ready)
        {
            _ = SeparateClipAsync();
        }
    }

    private async Awaitable SeparateClipAsync()
    {
        try
        {
            Debug.Log($"Loaded AudioClip '{audioClip.name}', length {audioClip.length}. Starting separation...");
            _ = await sourceSeparationComponent.SeparateClipAsync(audioClip);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Source separation failed: " + e.Message);
        }
    }

    private void OnError(string error)
    {
        Debug.LogError("Source separation error: " + error);
    }

    private void OnSeparationReady(SourceSeparationComponent.SeparatedClipSet result)
    {
        Debug.Log($"Separation ready! Source: {result.sourceName}, Model: {result.modelType}");
        foreach (SourceSeparationComponent.SeparatedStemClip stem in result.stems)
        {
            Debug.Log($"Stem: {stem.stemName}, Clip: {stem.clip.name}, Channels: {stem.channels}, SampleRate: {stem.sampleRate}, Length: {stem.clip.length} s");
        }
    }
}
