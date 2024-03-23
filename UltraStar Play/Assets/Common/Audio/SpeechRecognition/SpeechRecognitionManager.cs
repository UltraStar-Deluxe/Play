using System;
using System.Collections.Generic;
using System.Threading;
using UniInject;
using UnityEngine;
using Whisper;

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
        if (!IsWhisperSupportedOnHardware())
        {
            errorMessage = "Speech recognition is not yet supported on this hardware.\nPlease wait for a future release.";
            return false;
        }

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

    private bool IsWhisperSupportedOnHardware()
    {
        string avxCheckPath = ApplicationUtils.GetStreamingAssetsPath("AvxCheck/avx-check.exe");
        if (!FileUtils.Exists(avxCheckPath))
        {
            return true;
        }

        // Run the avx-check.exe to check if the hardware supports AVX instructions.
        // This is necessary because the Whisper library uses AVX instructions.
        try
        {
            if (ProcessUtils.RunProcess(avxCheckPath, "--json", out string processOutput, out string processError))
            {
                AvxCheckResultJson avxCheckResultJson = JsonConverter.FromJson<AvxCheckResultJson>(processOutput);
                bool isAvxSupported = avxCheckResultJson.avx > 0
                                    && avxCheckResultJson.avx2 > 0;
                Debug.Log($"isAvxSupported: {isAvxSupported}");
                return isAvxSupported;
            }
            else
            {
                Debug.Log($"Failed to run avx-check. Assuming AVX instructions are supported. Error Message: {processError}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to run avx-check.exe");
            return true;
        }
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
            parameters.SpeechRecognitionLanguage,
            parameters.Prompt);
        SpeechRecognizer speechRecognizer = new(parameters, whisperManager);
        parametersToSpeechRecognizer[parameters] = speechRecognizer;
        return speechRecognizer;
    }

    private WhisperManager CreateWhisperManager(string modelPath, string language, string prompt)
    {
        language = language.ToLowerInvariant();
        Debug.Log($"Creating WhisperManager with model '{modelPath}', modelPath: {modelPath}, prompt: {prompt}");

        WhisperManager whisperManager = Instantiate<WhisperManager>(whisperManagerPrefab, transform);
        whisperManager.name = $"WhisperManager language: {language}, modelPath: {modelPath}, prompt: {prompt}";
        whisperManager.IsModelPathInStreamingAssets = false;
        whisperManager.ModelPath = modelPath;
        whisperManager.language = language;
        whisperManager.initialPrompt = prompt;
        whisperManager.enableTokens = true;
        whisperManager.tokensTimestamps = true;
        whisperManager.translateToEnglish = false;
        whisperManager.singleSegment = false;
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

    private class AvxCheckResultJson
    {
        public int avx;
        public int avx2;
    }
}
