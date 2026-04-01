using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Eitan.Sherpa.Onnx.Unity.Mono.Components;
using Eitan.SherpaONNXUnity.Runtime;
using Eitan.SherpaONNXUnity.Runtime.Modules;
using UniInject;

public class OfflineSpeechRecognizerDemo : MonoBehaviour
{
    private const int ParakeetV3ExpectedSampleRate = 16000;
    
    [InjectedInInspector]
    public OfflineSpeechRecognizerComponent offlineRecognizer;

    void OnEnable()
    {
        if (offlineRecognizer != null)
        {
            offlineRecognizer.TranscriptReadyEvent.AddListener(HandleTranscriptReady);
            offlineRecognizer.TranscriptionFailedEvent.AddListener(HandleTranscriptionFailed);
            offlineRecognizer.InitializationStateChangedEvent.AddListener(HandleRecognizerReadyState);
            offlineRecognizer.FeedbackMessages.AddListener(HandleFeedbackMessage);
            offlineRecognizer.FeedbackReceived += HandleFeedback;
        }
    }
    
    void OnDisable()
    {
        if (offlineRecognizer != null)
        {
            offlineRecognizer.TranscriptReadyEvent.RemoveListener(HandleTranscriptReady);
            offlineRecognizer.TranscriptionFailedEvent.RemoveListener(HandleTranscriptionFailed);
            offlineRecognizer.InitializationStateChangedEvent.RemoveListener(HandleRecognizerReadyState);
            offlineRecognizer.FeedbackMessages.RemoveListener(HandleFeedbackMessage);
            offlineRecognizer.FeedbackReceived -= HandleFeedback;
        }
    }

    private async Awaitable TranscribeAsync(float[] audioSamples, int sampleRate, CancellationToken cancellationToken)
    {
        var samples = audioSamples.ToArray();
        var clip = AudioClip.Create("MonoAudioSamplesClip", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        
        WavFileWriter.WriteFile(ApplicationUtils.GetPersistentDataPath("MonoAudioSamples.wav"), sampleRate, clip.channels, samples);

        Debug.Log("Transcribing…");

        try
        {
            var result = await offlineRecognizer
                .TranscribeClipAsync(clip, cancellationToken).ConfigureAwait(true);
            if (result.Status != SpeechRecognition.TranscriptionStatus.Success
                || string.IsNullOrWhiteSpace(result.Text))
            {
                Debug.Log("No transcript returned.");
                return;
            }

            Debug.Log("Transcription complete.");
            HandleTranscriptReady(result);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void HandleTranscriptReady(SpeechRecognition.TranscriptionResult result)
    {
        var text = result.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var trimmedText = text.Trim();
        Debug.Log("Transcibed text: " + trimmedText);
    }

    private void HandleTranscriptionFailed(string message)
    {
        Debug.LogError(message);
    }
    
    private void HandleRecognizerReadyState(bool ready)
    {
        Debug.Log($"HandleRecognizerReadyState: {ready}");
        if (ready)
        {
            _ = TranscribeAudioAsync();
        }
    }
    
    private void HandleFeedbackMessage(string message)
    {
        Debug.Log($"HandleFeedbackMessage: {message}");
    }
    
    private void HandleFeedback(SherpaFeedback feedback)
    {
        Debug.Log($"HandleFeedback: {feedback.Message}");
    }
    
    private async Awaitable TranscribeAudioAsync()
    {
        string filePath =
            "C:/Dev/Projects/GitHub/achimmihca/VlcForUnityPlayground/Assets/StreamingAssets/Stone Sour - Through Glass - excerpt2.ogg";
        if (!File.Exists(filePath))
        {
            throw new Exception($"File does not exist: {filePath}");
        }

        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync($"file://{filePath}", false);
        Debug.Log("Loaded audioClip: " + audioClip);
        float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0, audioClip.length * 1000, true);
        float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(monoAudioSamples, audioClip.frequency, ParakeetV3ExpectedSampleRate);
        Debug.Log("Resampled mono audio samples: " + monoAudioSamplesResampled.Length);

        _ = TranscribeAsync(monoAudioSamplesResampled, ParakeetV3ExpectedSampleRate,
            new CancellationTokenSource(TimeSpan.FromMilliseconds(30000)).Token);
    }
}
