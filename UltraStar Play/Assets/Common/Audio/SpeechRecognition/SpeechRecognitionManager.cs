using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UniInject;
using UnityEngine;
using Whisper;
using Object = UnityEngine.Object;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection
{
    public static SpeechRecognitionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SpeechRecognitionManager>();

    [InjectedInInspector]
    public WhisperManager whisperManagerPrefab;
    
    [Inject]
    private Settings settings;

    private readonly Dictionary<SpeechRecognitionParameters, SpeechRecognizer> parametersToSpeechRecognizer = new();

    public SpeechRecognizer GetExistingSpeechRecognizer(SpeechRecognitionParameters parameters)
    {
        if (parametersToSpeechRecognizer.TryGetValue(parameters, out SpeechRecognizer speechRecognizer))
        {
            return speechRecognizer;
        }

        return null;
    }

    public bool TryInitExistingSpeechRecognizer(SpeechRecognitionParameters parameters, out string errorMessage)
    {
        if (parametersToSpeechRecognizer.TryGetValue(parameters, out SpeechRecognizer speechRecognizer))
        {
            if (!speechRecognizer.IsLoaded)
            {
                speechRecognizer.InitModel();
            }
            errorMessage = "";
            return true;
        }

        errorMessage = $"No speech recognizer found for parameters {parameters}";
        return false;
    }
    
    public bool TryGetOrCreateSpeechRecognizer(SpeechRecognitionParameters parameters, out string errorMessage, out SpeechRecognizer speechRecognizer)
    {
        if (parametersToSpeechRecognizer.TryGetValue(parameters, out speechRecognizer))
        {
            Log.Debug(() => $"Reusing cached speech recognizer for parameters {parameters}");
            errorMessage = "";
            return true;
        }

        string modelPath = parameters.ModelPath;
        if (modelPath.IsNullOrEmpty())
        {
            errorMessage = "Set the speech recognition model path first.";
            return false;
        }
        if (!FileUtils.Exists(modelPath))
        {
            errorMessage = "Speech recognition model path is not a valid file path.";
            return false;
        }
        
        speechRecognizer = CreateSpeechRecognizer(parameters);
        
        errorMessage = "";
        return true;
    }

    private SpeechRecognizer CreateSpeechRecognizer(SpeechRecognitionParameters parameters)
    {
        WhisperManager whisperManager = CreateWhisperManager(
            parameters.ModelPath,
            parameters.SpeechRecognitionLanguage);
        SpeechRecognizer speechRecognizer = new(parameters, whisperManager);
        parametersToSpeechRecognizer[parameters] = speechRecognizer;
        return speechRecognizer;
    }

    private WhisperManager CreateWhisperManager(string modelPath, string language)
    {
        language = language.ToLowerInvariant();
        Debug.Log($"Creating WhisperManager with model '{modelPath}' and language '{language}'");

        WhisperManager whisperManager = Instantiate<WhisperManager>(whisperManagerPrefab, transform);
        whisperManager.name = $"WhisperManager language: {language}, modelPath: {modelPath}";
        whisperManager.language = language;
        whisperManager.enableTokens = true;
        whisperManager.tokensTimestamps = true;
        whisperManager.translateToEnglish = false;
        whisperManager.singleSegment = false;
        whisperManager.IsModelPathInStreamingAssets = false;
        whisperManager.ModelPath = modelPath;
        return whisperManager;
    }

    private void OnApplicationQuit()
    {
        // Wait until the speech recognition process finished.
        SpeechRecognitionUtils.IsApplicationTerminating = true;
        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        long maxWaitDurationInMillis = 5000;
        while (SpeechRecognitionUtils.IsExternalSpeechRecognitionCallRunning
               && TimeUtils.GetUnixTimeMilliseconds() - startTime < maxWaitDurationInMillis)
        {
            Debug.Log("Waiting for speech recognition to finish");
            Thread.Sleep(500);
        }
    }
}
