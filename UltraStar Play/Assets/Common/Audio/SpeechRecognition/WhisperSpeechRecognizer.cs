using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Whisper;

public class WhisperSpeechRecognizer : SpeechRecognizer
{
    public override bool IsLoaded => whisperManager.IsLoaded;
    public override bool IsLoading => whisperManager.IsLoading;

    private readonly WhisperManager whisperManager;
    private readonly List<Action<double>> onProgressCallbacks = new();

    public WhisperSpeechRecognizer(SpeechRecognizerConfig config, WhisperManager whisperManager)
        : base(config)
    {
        this.whisperManager = whisperManager;
        this.whisperManager.OnProgress += OnProgress;
    }

    public override void InitModel()
    {
        // Blocking call to InitModel
        whisperManager.InitModel().GetAwaiter().GetResult();
    }

    public override async Awaitable<SpeechRecognitionResult> GetSpeechRecognitionResultAsync(
        SpeechRecognitionInputSamples samples,
        CancellationToken cancellationToken,
        Action<double> onProgress)
    {
        if (!whisperManager.IsLoaded
            && !whisperManager.IsLoading)
        {
            throw new Exception("Speech recognition model is not yet initialized.");
        }

        int lengthInSamples = samples.EndIndex - samples.StartIndex;
        float[] audioSamplesForSpeechRecognition = new float[lengthInSamples];
        Array.Copy(samples.MonoSamples, samples.StartIndex, audioSamplesForSpeechRecognition, 0, lengthInSamples);

        WhisperResult whisperResult;
        try
        {
            onProgressCallbacks.Add(onProgress);

            // Blocking call to GetTextAsync
            whisperResult = await whisperManager.GetTextAsync(audioSamplesForSpeechRecognition, samples.SampleRate, 1);
        }
        finally
        {
            onProgressCallbacks.Remove(onProgress);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (whisperResult != null
            && !whisperResult.Segments.IsNullOrEmpty())
        {
            TimeSpan offsetToStartIndex = TimeSpan.FromSeconds((double)samples.StartIndex / samples.SampleRate);
            string textResult = whisperResult.Result;
            List<SpeechRecognitionWordResult> wordResults = whisperResult.Segments
                // Whisper outputs special segments such as [Music], [BLANK_AUDIO], [NOISE], ♪, (sad music) etc. that are irrelevant for the lyrics.
                .Where(segment =>
                {
                    string trimmedText = segment.Text.Trim();
                    return IsValidLyrics(trimmedText);
                })
                .SelectMany(segment => segment.Tokens)
                .Where(token =>
                {
                    string trimmedText = token.Text.Trim();
                    return !token.IsSpecial
                           && IsValidLyrics(trimmedText);
                })
                .Select(token => new SpeechRecognitionWordResult(
                    token.Text,
                    token.Timestamp.Start + offsetToStartIndex,
                    token.Timestamp.End + offsetToStartIndex,
                    token.Prob))
                .ToList();
            if (wordResults.IsNullOrEmpty())
            {
                Debug.Log($"Speech recognition did not find any words");
            }
            else
            {
                SpeechRecognitionWordResult.NormalizeText(wordResults);
                wordResults = wordResults
                    .Where(wordResult => !StringUtils.IsOnlyWhitespace(wordResult.Text))
                    .ToList();
                Debug.Log($"Speech recognition result words: {wordResults.Select(it => $"'{it.Text}'").JoinWith("|")}");
            }
            return new SpeechRecognitionResult(textResult, wordResults);
        }

        return null;
    }

    private bool IsValidLyrics(string text)
    {
        return !(text.StartsWith("[") && text.EndsWith("]"))
            && !(text.StartsWith("(") && text.EndsWith(")"))
            && !(text.StartsWith("<") && text.EndsWith(">"))
            && !(text is "♪" or "♪ " or " ♪" or " ♪ ");
    }

    private void OnProgress(int progress)
    {
        Log.Debug(() => $"SpeechRecognizer progress: {progress}");
        foreach (Action<double> onProgressCallback in onProgressCallbacks)
        {
            onProgressCallback?.Invoke(progress);
        }
    }
}
