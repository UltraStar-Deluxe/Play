using System;
using System.Collections.Generic;
using UnityEngine;

// YIN implementation to find the fundamental frequency (aka. pitch detection).
// See the paper from Cheveigne and Kawahara: http://audition.ens.fr/adc/pdf/2002_JASA_YIN.pdf
public class YinAudioSamplesAnalyzer : AbstractAudioSamplesAnalyzer
{
    private const int DefaultBufferSize = 2048;
    // Default absolute threshold (Step 4 in paper)
    private const float DefaultThreshold = 0.1f;

    private const int MinSampleLength = 256;

    private readonly float threshold;
    private readonly int sampleRateHz;

    // Difference (Step 2)
    private readonly float[] diff;

    // Cumulative mean normalized difference (Step 3)
    private readonly float[] cmnDiff;

    private YinResult lastResult;

    public YinAudioSamplesAnalyzer(int sampleRateHz, int bufferSize = DefaultBufferSize, float threshold = DefaultThreshold)
    {
        this.sampleRateHz = sampleRateHz;
        this.threshold = threshold;

        this.diff = new float[bufferSize / 2];
        this.cmnDiff = new float[bufferSize / 2];
    }

    public override PitchEvent ProcessAudioSamples(float[] audioSamplesBuffer, int samplesSinceLastFrame, MicProfile micProfile)
    {
        if (!isEnabled)
        {
            Debug.LogWarning("AudioSamplesAnalyzer is disabled");
            return null;
        }

        if (samplesSinceLastFrame < MinSampleLength)
        {
            return null;
        }

        // Check if samples is louder than threshhold
        bool passesThreshold = IsAboveNoiseSuppressionThreshold(audioSamplesBuffer, micProfile);
        if (!passesThreshold)
        {
            return null;
        }

        YinResult yinResult = GetYinResult(audioSamplesBuffer);
        lastResult = yinResult;
        if (yinResult.probability <= 0)
        {
            return null;
        }

        // We are only interested in pitch values (approximately) inside of the singable audio spectrum.
        float pitchInHertz = yinResult.pitchInHertz;
        if (pitchInHertz < (MidiUtils.MinHalftoneFrequency / 2)
            || pitchInHertz > (MidiUtils.MaxHalftoneFrequency * 2))
        {
            return null;
        }

        int midiNote = GetBestMidiNoteForFrequencyInHertz(pitchInHertz);
        if (midiNote < 0)
        {
            return null;
        }

        return new PitchEvent(midiNote);
    }

    private int GetBestMidiNoteForFrequencyInHertz(float frequencyInHertz)
    {
        // TODO: Can this be calculated directly without searching through precalculated frequencies?
        int bestHalftone = -1;
        float bestFreqDiff = float.MaxValue;
        for (int halftone = 0; halftone < MidiUtils.halftoneFrequencies.Length; halftone++)
        {
            float freqDiff = Mathf.Abs((float)MidiUtils.halftoneFrequencies[halftone] - frequencyInHertz);
            if (freqDiff < bestFreqDiff)
            {
                bestFreqDiff = freqDiff;
                bestHalftone = halftone;
            }
        }

        if (bestHalftone < 0)
        {
            return -1;
        }
        return MidiUtils.MidiNoteMin + bestHalftone;
    }

    private YinResult GetYinResult(float[] audioBuffer)
    {
        YinResult result = new YinResult();

        // Step 2
        DifferenceFunction(audioBuffer, diff);

        // Step 3
        CumulativeMeanNormalizedDifferenceFunction(diff, cmnDiff);

        // Step 4
        // minimumIndex is the tau estimate.
        int minimumIndex = GetIndexOfMinimum(cmnDiff);
        if (minimumIndex == -1)
        {
            // No pitch found.
            // Consider using the last result, but with lower probabilty.
            if (lastResult != null)
            {
                result.pitchInHertz = lastResult.pitchInHertz;
                result.probability = lastResult.probability * 0.5f;
            }
            return result;
        }

        // The lower the difference, the higher the probability that the delay is correct.
        result.probability = 1 - cmnDiff[minimumIndex];

        // Step 6
        // This step is not implemented here.
        // In the paper it is searched for a Best Local Estimate.
        //
        // Here, we simply compare the probabilty with the last result.
        // If the last result has a significantly better probabilty,
        // then use this instead, but with lower probabilty than original.
        if (lastResult != null && lastResult.probability * 0.5f > result.probability)
        {
            result.pitchInHertz = lastResult.pitchInHertz;
            result.probability = lastResult.probability * 0.5f;
            return result;
        }

        // Step 5
        float betterMinimumIndex = ParabolicInterpolation(minimumIndex);

        // Conversion to Hz
        result.pitchInHertz = sampleRateHz / betterMinimumIndex;
        return result;
    }

    // Step 2
    private void DifferenceFunction(float[] input, float[] diff)
    {
        // Equation (6) of the paper.
        // d(tau) = sum_j=1_to_j=W [ ( x_j - x_(j+tau) )² ]
        float tmp;
        float sum;
        for (int tau = 1; tau < diff.Length; tau++)
        {
            sum = 0;
            for (int j = 0; j < diff.Length; j++)
            {
                // tmp := x_j - x_(j+tau)
                tmp = input[j] - input[j + tau];
                sum += (tmp * tmp);
            }
            diff[tau] = sum;
        }
    }

    // Step 3
    private void CumulativeMeanNormalizedDifferenceFunction(float[] diff, float[] cmdDiff)
    {
        // Equation (8) of the paper.
        // First case for (tau == 0): d'(tau) = 1
        cmdDiff[0] = 1;
        // Second case for (tau != 0): d'(tau) = [d(tau) / ((1 / tau) * sum)]
        // Note that the right side is equivalent to [d(tau) / (sum / tau)],
        // which is equivalent to [d(tau) * (tau / sum)]
        float sum = 0;
        for (int tau = 1; tau < cmdDiff.Length; tau++)
        {
            sum += diff[tau];
            cmdDiff[tau] = diff[tau] * tau / sum;
        }
    }

    // Step 4
    private int GetIndexOfMinimum(float[] cmnDiff)
    {
        float minValue = float.MaxValue;
        int minIndex = 0;
        bool previousWasFalling = false;
        bool isRising;
        bool foundMinimum = false;

        // Return global minimum or first minimum below threshold
        for (int i = 0; i < cmnDiff.Length - 1; i++)
        {
            isRising = (cmnDiff[i] < cmnDiff[i + 1]);
            // A minimum is found when the curve was falling and now begins to rise again
            if (previousWasFalling && isRising
                && cmnDiff[i] < minValue)
            {
                foundMinimum = true;
                minValue = cmnDiff[i];
                minIndex = i;
                if (minValue < threshold)
                {
                    break;
                }
            }

            previousWasFalling = !isRising;
        }

        if (!foundMinimum)
        {
            return -1;
        }
        return minIndex;
    }

    // Step 5
    private float ParabolicInterpolation(int minimumIndex)
    {
        float minimumValue = diff[minimumIndex];

        // Index of the minimum in the diff array could be slightly offset from the index in the cmnDiff array.
        // Search minimum in diff array "to the left"
        for (int i = minimumIndex; i > 0; i--)
        {
            if (diff[i] < minimumValue)
            {
                minimumIndex = i;
                minimumValue = diff[i];
            }
            else
            {
                break;
            }
        }
        // Search minimum in diff array "to the right"
        for (int i = minimumIndex; i < diff.Length; i++)
        {
            if (diff[i] < minimumValue)
            {
                minimumIndex = i;
                minimumValue = diff[i];
            }
            else
            {
                break;
            }
        }

        // General parabolic interpolation: http://fourier.eng.hmc.edu/e176/lectures/NM/node25.html
        // For YIN, the parabolic interpolation is done with the direct neighbors (index a and c) of the minimum (index b),
        int b = minimumIndex;
        int a = Math.Max(b - 1, 0);
        int c = Math.Min(b + 1, diff.Length - 1);

        // With only two points (first point is equal to second point, so there are only two points),
        // parabolic interpolation is not possible. In this case, just return index with smallest value.
        if (a == b)
        {
            return (diff[b] <= diff[c]) ? b : c;
        }

        if (c == b)
        {
            return (diff[b] <= diff[a]) ? b : a;
        }

        // Perform parabolic interpolation and return the index with the lowest point on the curve.
        float diffA = diff[a];
        float diffB = diff[b];
        float diffC = diff[c];
        float betterMinimumIndex = b + (diffC - diffA) / (2 * (2 * diffB - diffC - diffA));
        return betterMinimumIndex;
    }

    private class YinResult
    {
        public float pitchInHertz = -1;

        // Propability that the found pitch is correct.
        // 0 means the found pitch is certainly wrong, 1 means the found pitch is certainly correct.
        public float probability;
    }
}
