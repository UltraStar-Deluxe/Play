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

    public SpeechRecognizer(SpeechRecognitionParameters speechRecognitionParameters, WhisperManager whisperManager)
    {
        this.SpeechRecognitionParameters = speechRecognitionParameters;
        this.whisperManager = whisperManager;
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
            InitModel();
        }
        
        int lengthInSamples = endIndex - startIndex;
        float[] audioSamplesForSpeechRecognition = new float[lengthInSamples];
        Array.Copy(monoSamples, startIndex, audioSamplesForSpeechRecognition, 0, lengthInSamples);
        
        // Blocking call to GetTextAsync
        WhisperResult whisperResult = whisperManager.GetTextAsync(audioSamplesForSpeechRecognition, sampleRate, 1)
            .Result;

        cancellationToken.ThrowIfCancellationRequested();
        
        SpeechRecognitionResult speechRecognitionResult;
        if (whisperResult != null
            && !whisperResult.Segments.IsNullOrEmpty()
            && !whisperResult.Result.StartsWith("[BLANK_AUDIO]", StringComparison.InvariantCultureIgnoreCase)
            && !whisperResult.Result.StartsWith("[Music]", StringComparison.InvariantCultureIgnoreCase))
        {
            TimeSpan offsetToStartIndex = TimeSpan.FromSeconds((double)startIndex / sampleRate);
            string textResult = whisperResult.Result;
            List<SpeechRecognitionWordResult> wordResults = whisperResult.Segments
                .SelectMany(whisperSegment => whisperSegment.Tokens)
                .Where(token => !token.IsSpecial && !token.Text.TrimStart().StartsWith("[") && !token.Text.TrimEnd().EndsWith("]"))
                .Select(token => new SpeechRecognitionWordResult(
                    token.Text,
                    token.Timestamp.Start + offsetToStartIndex,
                    token.Timestamp.End + offsetToStartIndex))
                .ToList();
            SpeechRecognitionWordResult.NormalizeText(wordResults);
            wordResults = wordResults
                .Where(wordResult => !StringUtils.IsOnlyWhitespace(wordResult.Text))
                .ToList();
            Debug.Log($"Speech recognition result words: {wordResults.Select(it => $"'{it.Text}'").JoinWith("|")}");
            return new SpeechRecognitionResult(textResult, wordResults);
        }

        return null;
    }

    public void InitModel()
    {
        // Blocking call to InitModel
        whisperManager.InitModel().GetAwaiter().GetResult();
    }
}
