using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Whisper;

public class SpeechRecognizer
{
    public SpeechRecognitionParameters SpeechRecognitionParameters { get; private set; }
    public bool IsLoaded => whisperManager.IsLoaded;
    public bool IsLoading => whisperManager.IsLoading;

    private readonly WhisperManager whisperManager;

    private readonly List<Action<double>> onProgressCallbacks = new();

    public SpeechRecognizer(SpeechRecognitionParameters speechRecognitionParameters, WhisperManager whisperManager)
    {
        this.SpeechRecognitionParameters = speechRecognitionParameters;
        this.whisperManager = whisperManager;
        this.whisperManager.OnProgress += OnProgress;
    }

    private void OnProgress(int progress)
    {
        Log.Debug(() => $"SpeechRecognizer progress: {progress}");
        foreach (Action<double> onProgressCallback in onProgressCallbacks)
        {
            onProgressCallback?.Invoke(progress);
        }
    }

    public SpeechRecognitionResult GetSpeechRecognitionResult(
        float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate,
        CancellationToken cancellationToken,
        Action<double> onProgress)
    {
        if (!whisperManager.IsLoaded
            && !whisperManager.IsLoading)
        {
            throw new Exception("Speech recognition model is not yet initialized.");
        }
        
        int lengthInSamples = endIndex - startIndex;
        float[] audioSamplesForSpeechRecognition = new float[lengthInSamples];
        Array.Copy(monoSamples, startIndex, audioSamplesForSpeechRecognition, 0, lengthInSamples);

        WhisperResult whisperResult;
        try
        {
            onProgressCallbacks.Add(onProgress);

            // Blocking call to GetTextAsync
            whisperResult = whisperManager.GetTextAsync(audioSamplesForSpeechRecognition, sampleRate, 1)
                .Result;
        }
        finally
        {
            onProgressCallbacks.Remove(onProgress);
        }

        cancellationToken.ThrowIfCancellationRequested();
        
        if (whisperResult != null
            && !whisperResult.Segments.IsNullOrEmpty())
        {
            TimeSpan offsetToStartIndex = TimeSpan.FromSeconds((double)startIndex / sampleRate);
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

    public void InitModel()
    {
        // Blocking call to InitModel
        whisperManager.InitModel().GetAwaiter().GetResult();
    }
}
