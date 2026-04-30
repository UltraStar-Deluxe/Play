using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Windows;
using NWaves.Filters.Fda;
using NWaves.Signals;
using NWaves.Operations;

public class NemoForcedAligner : IDisposable
{
    public const int SampleRate = 16000;

    private readonly string[] tokens;
    private readonly Dictionary<string, int> tokenToId;
    private readonly InferenceSession session;
    private readonly int blankIndex;

    public NemoForcedAligner(string modelPath, string tokensPath)
    {
        session = new InferenceSession(modelPath);
        tokens = File.ReadAllLines(tokensPath)
            .Where(l => l.Length > 0)
            .ToArray();
        tokenToId = new Dictionary<string, int>();
        for (int i = 0; i < tokens.Length; i++)
        {
            tokenToId[tokens[i]] = i;
        }

        blankIndex = tokens.Length;
    }

    public void Dispose()
    {
        session?.Dispose();
    }

    /// <summary>
    /// Runs forced alignment for the given audio and transcript.
    /// Assumes audio is 16kHz Mono (resampled if necessary).
    /// Assumes the ONNX model has an input 'audio_signal' and 'length', and output 'logprobs'.
    /// </summary>
    public ForcedAlignmentResult Run(AudioData audioData, string transcript)
    {
        var features = ExtractFeatures(audioData);
        var logprobs = RunInference(features);
        var targetIds = Tokenize(transcript);

        Console.WriteLine(
            $"[DEBUG_LOG] Transcript: {(transcript.Length > 100 ? transcript.Substring(0, 100) + "..." : transcript)}");
        Console.WriteLine($"[DEBUG_LOG] Token IDs (first 100): {string.Join(", ", targetIds.Take(100))}");
        Console.WriteLine(
            $"[DEBUG_LOG] Tokens (first 100): {string.Join(", ", targetIds.Take(100).Select(id => tokens[id]))}");

        var path = ViterbiAlign(logprobs, targetIds);

        // Calculate frame duration dynamically.
        // Assumption: Features use 10ms hop (160 samples at 16kHz).
        // The model downsamples these features by some factor (usually 4 or 8).
        double inputFrameDuration = 0.01; // 10ms
        int numInputFrames = features.FrameCount;
        int numOutputFrames = logprobs.Data.GetLength(0);

        // We use the same logic as the Python implementation to calculate the output timestep duration.
        // But we avoid rounding the downsampling factor to keep maximum precision.
        double outputFrameDuration = inputFrameDuration * numInputFrames / numOutputFrames;


        int S = 2 * targetIds.Count + 1;
        var alignment = GetAlignment(path, targetIds, outputFrameDuration, S);
        return alignment;
    }

    private List<int> Tokenize(string text)
    {
        text = text.Replace(" ", "▁");
        if (!text.StartsWith("▁")) text = "▁" + text;

        var result = new List<int>();
        int pos = 0;
        while (pos < text.Length)
        {
            bool found = false;
            for (int len = Math.Min(text.Length - pos, 20); len > 0; len--)
            {
                string sub = text.Substring(pos, len);
                if (tokenToId.TryGetValue(sub, out int id))
                {
                    result.Add(id);
                    pos += len;
                    found = true;
                    break;
                }

                // Try lowercase if original case is not found
                string subLower = sub.ToLowerInvariant();
                if (subLower != sub && tokenToId.TryGetValue(subLower, out id))
                {
                    result.Add(id);
                    pos += len;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // If even lowercase is not found, we skip the character to avoid infinite loop.
                // This might happen for punctuation or characters not in the vocabulary at all.
                pos++;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts Mel Spectrogram features.
    /// Assumption: 80 mel bins, 400 sample window (25ms), 160 sample hop (10ms).
    /// Matches NeMo's default preprocessor configuration for many models (e.g. Conformer).
    /// </summary>
    private AudioFeatures ExtractFeatures(AudioData audioData)
    {
        float[] samples = PrepareSamples(audioData);

        var options = CreateFilterbankOptions();
        var extractor = new FilterbankExtractor(options);
        var signal = new DiscreteSignal(SampleRate, samples);
        var features = extractor.ComputeFrom(signal);

        ApplyLog(features);
        Normalize(features);

        return new AudioFeatures { Data = features.ToArray() };
    }

    private float[] PrepareSamples(AudioData audioData)
    {
        float[] samples = audioData.Samples;

        if (audioData.ChannelCount > 1)
        {
            samples = ConvertToMono(samples, audioData.ChannelCount);
        }

        if (audioData.SampleRate != SampleRate)
        {
            samples = Resample(samples, audioData.SampleRate);
        }

        return samples;
    }

    private float[] ConvertToMono(float[] samples, int channelCount)
    {
        float[] monoSamples = new float[samples.Length / channelCount];
        for (int i = 0; i < monoSamples.Length; i++)
        {
            float sum = 0;
            for (int c = 0; c < channelCount; c++)
            {
                sum += samples[i * channelCount + c];
            }

            monoSamples[i] = sum / channelCount;
        }

        return monoSamples;
    }

    private float[] Resample(float[] samples, int sourceSampleRate)
    {
        var inputSignal = new DiscreteSignal(sourceSampleRate, samples);
        var resampled = Operation.Resample(inputSignal, SampleRate);
        return resampled.Samples;
    }

    private FilterbankOptions CreateFilterbankOptions()
    {
        return new FilterbankOptions
        {
            SamplingRate = SampleRate,
            FrameSize = 400,
            HopSize = 160,
            Window = WindowType.Hann,
            FilterBankSize = 80,
            LowFrequency = 0,
            HighFrequency = 8000,
            FftSize = 512,
            FilterBank = FilterBanks.Triangular(512, SampleRate, FilterBanks.MelBands(80, SampleRate, 0, 8000))
        };
    }

    private void ApplyLog(List<float[]> features)
    {
        foreach (var frame in features)
        {
            for (int i = 0; i < frame.Length; i++)
            {
                frame[i] = (float)Math.Log(Math.Max(frame[i], 1e-5));
            }
        }
    }

    private void Normalize(List<float[]> features)
    {
        if (features.Count == 0) return;

        int numFrames = features.Count;
        int numBins = features[0].Length;

        for (int b = 0; b < numBins; b++)
        {
            double sum = 0;
            double sumSq = 0;
            for (int t = 0; t < numFrames; t++)
            {
                float val = features[t][b];
                sum += val;
                sumSq += val * val;
            }

            double mean = sum / numFrames;
            double std = Math.Sqrt(Math.Max(0, (sumSq / numFrames) - (mean * mean))) + 1e-5;

            for (int t = 0; t < numFrames; t++)
            {
                features[t][b] = (float)((features[t][b] - mean) / std);
            }
        }
    }

    private LogProbs RunInference(AudioFeatures features)
    {
        int numFrames = features.FrameCount;
        int numBins = features.FeatureCount;

        var tensor = new DenseTensor<float>(new[] { 1, numBins, numFrames });
        for (int t = 0; t < numFrames; t++)
        {
            for (int b = 0; b < numBins; b++)
            {
                tensor[0, b, t] = features.Data[t][b];
            }
        }

        var lengthTensor = new DenseTensor<long>(new[] { 1 });
        lengthTensor[0] = numFrames;


        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("audio_signal", tensor),
            NamedOnnxValue.CreateFromTensor("length", lengthTensor)
        };

        using (var results = session.Run(inputs))
        {
            var logprobsTensor = results.First(r => r.Name == "logprobs").AsTensor<float>();
            int outFrames = logprobsTensor.Dimensions[1];
            int vocabSize = logprobsTensor.Dimensions[2];


            var output = new float[outFrames, vocabSize];
            for (int t = 0; t < outFrames; t++)
            {
                for (int v = 0; v < vocabSize; v++)
                {
                    output[t, v] = logprobsTensor[0, t, v];
                }
            }

            return new LogProbs { Data = output };
        }
    }

    /// <summary>
    /// Performs Viterbi decoding to find the most likely alignment path.
    /// The path consists of state indices in the 'augmented' sequence.
    /// </summary>
    private int[] ViterbiAlign(LogProbs logProbs, List<int> targetIds)
    {
        float[,] logprobs = logProbs.Data;
        int T = logprobs.GetLength(0);
        int[] augmented = new int[2 * targetIds.Count + 1];
        for (int i = 0; i < targetIds.Count; i++)
        {
            augmented[2 * i] = blankIndex;
            augmented[2 * i + 1] = targetIds[i];
        }

        augmented[augmented.Length - 1] = blankIndex;

        int S = augmented.Length;
        float[,] dp = new float[T, S];
        int[,] backtrack = new int[T, S];

        for (int t = 0; t < T; t++)
        for (int s = 0; s < S; s++)
            dp[t, s] = float.NegativeInfinity;

        dp[0, 0] = logprobs[0, augmented[0]];
        dp[0, 1] = logprobs[0, augmented[1]];

        for (int t = 1; t < T; t++)
        {
            for (int s = 0; s < S; s++)
            {
                // Stay in same state
                float bestPrevLogProb = dp[t - 1, s];
                int bestPrevState = s;

                // From s-1
                if (s > 0 && dp[t - 1, s - 1] > bestPrevLogProb)
                {
                    bestPrevLogProb = dp[t - 1, s - 1];
                    bestPrevState = s - 1;
                }

                // From s-2 (if current is not blank and s-1 is blank and target[s] != target[s-2])
                if (s > 1 && augmented[s] != blankIndex && augmented[s] != augmented[s - 2])
                {
                    if (dp[t - 1, s - 2] > bestPrevLogProb)
                    {
                        bestPrevLogProb = dp[t - 1, s - 2];
                        bestPrevState = s - 2;
                    }
                }

                if (!float.IsNegativeInfinity(bestPrevLogProb))
                {
                    dp[t, s] = bestPrevLogProb + logprobs[t, augmented[s]];
                    backtrack[t, s] = bestPrevState;
                }
            }
        }

        int[] path = new int[T];
        int currentS = (dp[T - 1, S - 1] > dp[T - 1, S - 2]) ? S - 1 : S - 2;
        for (int t = T - 1; t >= 0; t--)
        {
            path[t] = currentS;
            currentS = backtrack[t, currentS];
        }


        return path;
    }

    /// <summary>
    /// Converts the Viterbi path into word and token timestamps.
    /// </summary>
    private ForcedAlignmentResult GetAlignment(int[] path, List<int> targetIds, double frameDuration, int S)
    {
        FindAppearances(path, S, out var firstAppearance, out var lastAppearance);

        var tokenTimestamps = GetTokenTimestamps(targetIds, frameDuration, firstAppearance, lastAppearance);
        var wordTimestamps = AggregateWords(tokenTimestamps);

        return new ForcedAlignmentResult { Words = wordTimestamps };
    }

    private void FindAppearances(int[] path, int S, out int[] firstAppearance, out int[] lastAppearance)
    {
        firstAppearance = new int[S];
        lastAppearance = new int[S];
        for (int s = 0; s < S; s++)
        {
            firstAppearance[s] = -1;
            lastAppearance[s] = -1;
        }

        for (int t = 0; t < path.Length; t++)
        {
            int s = path[t];
            if (firstAppearance[s] == -1) firstAppearance[s] = t;
            lastAppearance[s] = t;
        }
    }

    private List<TokenTimestamp> GetTokenTimestamps(List<int> targetIds, double frameDuration, int[] firstAppearance,
        int[] lastAppearance)
    {
        var tokenTimestamps = new List<TokenTimestamp>();
        for (int i = 0; i < targetIds.Count; i++)
        {
            int stateIdx = 2 * i + 1;
            int startFrame = firstAppearance[stateIdx];
            int endFrame = lastAppearance[stateIdx];

            if (startFrame == -1)
            {
                // Fallback: use the end of the previous state or start of the next
                startFrame = (stateIdx > 0) ? lastAppearance[stateIdx - 1] + 1 : 0;
                endFrame = startFrame;
            }

            tokenTimestamps.Add(new TokenTimestamp
            {
                Token = tokens[targetIds[i]],
                StartTime = startFrame * frameDuration,
                EndTime = (endFrame + 1) * frameDuration
            });
        }

        return tokenTimestamps;
    }

    private List<WordTimestamp> AggregateWords(List<TokenTimestamp> tokenTimestamps)
    {
        var wordTimestamps = new List<WordTimestamp>();
        foreach (var tt in tokenTimestamps)
        {
            if (tt.Token.StartsWith("▁"))
            {
                var wt = new WordTimestamp
                {
                    Word = tt.Token.Substring(1), StartTime = tt.StartTime, EndTime = tt.EndTime
                };
                wt.Tokens.Add(tt);
                wordTimestamps.Add(wt);
            }
            else if (wordTimestamps.Count > 0)
            {
                var lastWord = wordTimestamps.Last();
                lastWord.Word += tt.Token;
                lastWord.EndTime = tt.EndTime;
                lastWord.Tokens.Add(tt);
            }
        }

        return wordTimestamps;
    }

    public class ForcedAlignmentResult
    {
        public List<WordTimestamp> Words { get; set; } = new List<WordTimestamp>();

        public ForcedAlignmentResult Clone()
        {
            var result = new ForcedAlignmentResult();
            foreach (var w in Words)
            {
                result.Words.Add(w.Clone());
            }

            return result;
        }
    }

    public class TokenTimestamp
    {
        public string Token { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public override string ToString() => $"'{Token}': {StartTime:F2} - {EndTime:F2}";

        public TokenTimestamp Clone()
        {
            return new TokenTimestamp { Token = Token, StartTime = StartTime, EndTime = EndTime };
        }
    }

    public class WordTimestamp
    {
        public string Word { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public List<TokenTimestamp> Tokens { get; set; } = new List<TokenTimestamp>();
        public override string ToString() => $"{Word}: {StartTime:F2} - {EndTime:F2} ({Tokens.Count} tokens)";

        public WordTimestamp Clone()
        {
            var clone = new WordTimestamp { Word = Word, StartTime = StartTime, EndTime = EndTime };
            foreach (var t in Tokens)
            {
                clone.Tokens.Add(t.Clone());
            }

            return clone;
        }
    }

    public class AudioData
    {
        public float[] Samples { get; set; }
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
    }


    private class LogProbs
    {
        public float[,] Data { get; set; }

        public int FrameCount => Data?.GetLength(0) ?? 0;
        public int VocabSize => Data?.GetLength(1) ?? 0;
    }

    private class AudioFeatures
    {
        public float[][] Data { get; set; }

        public int FrameCount => Data?.Length ?? 0;
        public int FeatureCount => (Data != null && Data.Length > 0) ? Data[0].Length : 0;
    }
}
