using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UniInject;
using UnityEngine;
using Whisper;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection
{
    public static SpeechRecognitionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SpeechRecognitionManager>();

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
        Debug.Log($"Creating WhisperManager with model '{modelPath}' and language '{language}'");

        GameObject whisperManagerGameObject = new GameObject($"WhisperManager language: {language}, modelPath: {modelPath}");
        whisperManagerGameObject.transform.parent = transform;
        
        WhisperManager whisperManager = whisperManagerGameObject.AddComponent<WhisperManager>();
        whisperManager.language = language;
        whisperManager.enableTokens = true;
        whisperManager.tokensTimestamps = true;
        whisperManager.translateToEnglish = false;
        whisperManager.singleSegment = false;
        SetWhisperModelPath(whisperManager, modelPath);
        return whisperManager;
    }

    private void SetWhisperModelPath(WhisperManager whisperManager, string modelPath)
    {
        // Set the model field via reflection, because the property is private.
        FieldInfo prop = whisperManager
            .GetType()
            .GetField("modelPath",
                System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance);
        prop.SetValue(whisperManager, modelPath);
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
