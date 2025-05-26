#if UNITY_ANDROID

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/**
 * Dummy implementation of used Unity-Whisper methods.
 * Can be used when building for platforms that Unity-Whisper does not support yet,
 * for example Android.
 */
namespace Whisper
{
    public class WhisperManager :MonoBehaviour
    {
        public string name;
        public string language;
        public string initialPrompt;
        public bool enableTokens;
        public bool tokensTimestamps;
        public bool translateToEnglish;
        public bool singleSegment;
        public bool IsModelPathInStreamingAssets { get; set; }
        public string ModelPath { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
        public Action<int> OnProgress { get; set; }

        public async Task<WhisperResult> GetTextAsync(float[] audioSamplesForSpeechRecognition, int sampleRate, int i)
        {
            return null;
        }

        public async Task InitModel()
        {
        }
    }

    public class WhisperResult
    {
        public List<WhisperResultSegment> Segments { get; set; }
        public string Result { get; set; }
    }

    public class WhisperResultSegment
    {
        public string Text { get; set; }
        public List<WhisperResultToken> Tokens { get; set; }
    }

    public class WhisperResultToken
    {
        public string Text { get; set; }
        public bool IsSpecial { get; set; }
        public WhisperResultTimestamp Timestamp { get; set; }
        public double Prob { get; set; }
    }

    public class WhisperResultTimestamp
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}

#endif
