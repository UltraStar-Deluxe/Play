using System;
using System.Threading;
using UnityEngine;

public abstract class SpeechRecognizer
{
    public SpeechRecognizerConfig Config { get; private set; }
    public abstract bool IsLoaded { get; }
    public abstract bool IsLoading { get; }

    protected SpeechRecognizer(SpeechRecognizerConfig config)
    {
        this.Config = config;
    }

    public abstract void InitModel();

    public abstract Awaitable<SpeechRecognitionResult> GetSpeechRecognitionResultAsync(
        SpeechRecognitionInputSamples samples,
        CancellationToken cancellationToken,
        Action<double> onProgress);
}
