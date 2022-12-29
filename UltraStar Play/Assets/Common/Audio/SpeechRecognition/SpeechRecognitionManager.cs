using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UniInject;
using UniRx;
using Vosk;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SpeechRecognitionManager : MonoBehaviour, INeedInjection, IDisposable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        instance = null;
    }

    private static SpeechRecognitionManager instance;
    public static SpeechRecognitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                SpeechRecognitionManager instanceInScene = GameObjectUtils.FindComponentWithTag<SpeechRecognitionManager>("SpeechRecognitionManager");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    [Inject]
    private UiManager uiManager;

    private VoskModelParameters lastVoskModelParameters;
    private Model voskModel;
    private VoskRecognizer voskRecognizer;

    public VoskRecognizer GetSpeechRecognizer(VoskModelParameters voskModelParameters)
    {
        CreateOrUpdateSpeechRecognizer(voskModelParameters);
        return voskRecognizer;
    }

    public void CreateOrUpdateSpeechRecognizer(VoskModelParameters voskModelParameters)
    {
        // Update the model
        using (new DisposableStopwatch("Create speech recognition model took <ms>"))
        {
            if (!TryCreateOrUpdateVoskModel(voskModelParameters))
            {
                return;
            }
        }

        using (new DisposableStopwatch("Create speech recognizer took <ms>"))
        {
            CreateOrUpdateVoskRecognizer(voskModelParameters);
        }

        lastVoskModelParameters = voskModelParameters;
    }

    private void CreateOrUpdateVoskRecognizer(VoskModelParameters voskModelParameters)
    {
        if (voskModel == null)
        {
            throw new IllegalStateException("VoskModel is null");
        }
        if (voskModelParameters.SampleRate <= 0)
        {
            throw new IllegalStateException("Invalid sample rate");
        }

        // Vosk always expects a new recognizer object for a new stream
        // See https://github.com/alphacep/vosk-api/issues/919
        voskRecognizer?.Dispose();
        if (!voskModelParameters.Phrases.IsNullOrEmpty())
        {
            string voskGrammar = JsonConverter.ToJson(voskModelParameters.Phrases);
            voskRecognizer = new(voskModel, voskModelParameters.SampleRate, voskGrammar);
        }
        else
        {
            voskRecognizer = new(voskModel, voskModelParameters.SampleRate);
        }

        voskRecognizer.SetWords(true);
    }

    private bool TryCreateOrUpdateVoskModel(VoskModelParameters voskModelParameters)
    {
        if (voskModelParameters.ModelPath.IsNullOrEmpty())
        {
            uiManager.CreateNotificationVisualElement("Set the speech recognition model path first.");
            return false;
        }
        if (!Directory.Exists(voskModelParameters.ModelPath))
        {
            uiManager.CreateNotificationVisualElement("Speech recognition model path is not a valid folder path.");
            return false;
        }

        if (voskModelParameters.ModelParametersEquals(lastVoskModelParameters)
            && voskModel != null)
        {
            // Use the previous version
            return true;
        }

        Debug.Log($"Loading speech recognition model from {voskModelParameters.ModelPath}");
        voskModel?.Dispose();
        voskModel = new(voskModelParameters.ModelPath);
        return true;
    }

    public void Dispose()
    {
        voskModel?.Dispose();
        voskRecognizer?.Dispose();
    }

    public void OnDestroy()
    {
        Dispose();
    }
}
