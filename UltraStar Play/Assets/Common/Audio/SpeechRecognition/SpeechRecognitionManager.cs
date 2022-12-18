using System;
using System.Collections;
using System.Collections.Generic;
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
        if (voskModelParameters.Equals(lastVoskModelParameters))
        {
            // Use previous version
            return;
        }

        // Update the model
        using (new DisposableStopwatch("Create speech recognition model took <ms>"))
        {
            CreateOrUpdateVoskModel(voskModelParameters);
        }

        using (new DisposableStopwatch("Create speech recognizer took <ms>"))
        {
            CreateOrUpdateVoskRecognizer(voskModelParameters);
        }

        lastVoskModelParameters = voskModelParameters;
    }

    private void CreateOrUpdateVoskRecognizer(VoskModelParameters voskModelParameters)
    {
        if (voskModelParameters.RecognizerParametersEquals(lastVoskModelParameters))
        {
            // Nothing changed, use old recognizer
            return;
        }

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

        // TODO: Set to true for timing information of the words, which can then be used to map them to notes
        // voskRecognizer.SetWords(true);
    }

    private void CreateOrUpdateVoskModel(VoskModelParameters voskModelParameters)
    {
        if (voskModelParameters.ModelParametersEquals(lastVoskModelParameters)
            && voskModel != null)
        {
            // Use the previous version
            return;
        }

        voskModel?.Dispose();
        voskModel = new(voskModelParameters.ModelPath);
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
