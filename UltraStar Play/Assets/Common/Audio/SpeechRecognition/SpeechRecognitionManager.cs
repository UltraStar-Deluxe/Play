using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UniInject;
using UnityEngine;
using Vosk;
using Whisper;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection, IDisposable
{
    public static SpeechRecognitionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SpeechRecognitionManager>();

    [field: Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public WhisperManager WhisperManager { get; private set; }

    private readonly Dictionary<string, Model> pathToSpeechRecognitionModel = new();
    private VoskRecognizer lastVoskRecognizer;

    public bool HasLoadedSpeechRecognitionModel(string modelPath)
    {
        // return pathToSpeechRecognitionModel.ContainsKey(modelPath);
        return WhisperManager.IsLoaded;
    }

    public VoskRecognizer CreateSpeechRecognizer(SpeechRecognitionParameters speechRecognitionParameters)
    {
        // Load the model if needed
        if (!HasLoadedSpeechRecognitionModel(speechRecognitionParameters.ModelPath))
        {
            using (new DisposableStopwatch("Create speech recognition model took <ms>"))
            {
                if (!TryLoadSpeechRecognitionModel(speechRecognitionParameters.ModelPath, out string errorMessage))
                {
                    throw new IllegalStateException(errorMessage);
                }
            }
        }
        Model speechRecognitionModel = pathToSpeechRecognitionModel[speechRecognitionParameters.ModelPath];

        using (new DisposableStopwatch("Create speech recognizer took <ms>"))
        {
            if (!HasLoadedSpeechRecognitionModel(speechRecognitionParameters.ModelPath))
            {
                throw new IllegalStateException("Speech recognition model not loaded");
            }
            if (speechRecognitionParameters.SampleRate <= 0)
            {
                throw new IllegalStateException("Invalid sample rate");
            }

            // Vosk always expects a new recognizer object for a new stream
            // See https://github.com/alphacep/vosk-api/issues/919
            lastVoskRecognizer?.Dispose();
            if (!speechRecognitionParameters.Phrases.IsNullOrEmpty())
            {
                string voskGrammar = JsonConverter.ToJson(speechRecognitionParameters.Phrases);
                lastVoskRecognizer = new(speechRecognitionModel, speechRecognitionParameters.SampleRate, voskGrammar);
            }
            else
            {
                lastVoskRecognizer = new(speechRecognitionModel, speechRecognitionParameters.SampleRate);
            }

            lastVoskRecognizer.SetWords(true);
        }

        return lastVoskRecognizer;
    }

    public bool TryLoadSpeechRecognitionModel(string modelPath, out string errorMessage)
    {
        if (HasLoadedSpeechRecognitionModel(modelPath))
        {
            // Nothing to do
            errorMessage = "";
            return true;
        }

        if (WhisperManager.IsLoading)
        {
            errorMessage = "Speech recognition model is still loading.";
            return false;
        }
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

        Debug.Log($"Loading speech recognition model from {modelPath}");
        SetWhisperModelPath(WhisperManager, modelPath);
        WhisperManager.language = "en";
        WhisperManager.enableTokens = true;
        WhisperManager.tokensTimestamps = true;
        WhisperManager.translateToEnglish = false;
        WhisperManager.singleSegment = false;
        WhisperManager.InitModel();
        // Model speechRecognitionModel = new(modelPath);
        // pathToSpeechRecognitionModel[modelPath] = speechRecognitionModel;
        errorMessage = "";
        return true;
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

    public void Dispose()
    {
        lastVoskRecognizer?.Dispose();
        pathToSpeechRecognitionModel.Values.ForEach(model => model?.Dispose());
        pathToSpeechRecognitionModel.Clear();
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

        Dispose();
    }
}
