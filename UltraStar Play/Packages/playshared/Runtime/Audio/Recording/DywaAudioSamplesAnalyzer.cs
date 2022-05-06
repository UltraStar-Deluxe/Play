using System;
using UnityEngine;

public class DywaAudioSamplesAnalyzer : AbstractAudioSamplesAnalyzer
{
    private const int MinSampleLength = 256;
    private readonly int maxSampleLength;

    private readonly DywaPitchTracker dywaPitchTracker;
    private readonly float[] halftoneFrequencies;

    public DywaAudioSamplesAnalyzer(int sampleRateHz, int maxSampleLength)
    {
        this.maxSampleLength = maxSampleLength;
        halftoneFrequencies = MidiUtils.PrecalculateHalftoneFrequencies(MidiUtils.SingableNoteMin, MidiUtils.SingableNoteRange);

        // Create and configure Dynamic Wavelet Pitch Tracker.
        dywaPitchTracker = new DywaPitchTracker();
        dywaPitchTracker.SampleRateHz = sampleRateHz;
    }

    public override PitchEvent ProcessAudioSamples(float[] sampleBuffer, int startIndexInclusive, int endIndexExclusive, MicProfile micProfile)
    {
        int sampleLength = endIndexExclusive - startIndexInclusive;
        if (sampleLength < MinSampleLength)
        {
            return null;
        }
        int sampleCountToUse = PreviousPowerOfTwo(sampleLength);

        // The number of analyzed samples impacts the performance notably.
        // Do not analyze more samples than necessary.
        if (sampleCountToUse > maxSampleLength)
        {
            sampleCountToUse = PreviousPowerOfTwo(maxSampleLength);
        }

        // Copy samples if needed
        if (!ModifySamplesInPlace)
        {
            // Copy original sample buffer
            float[] sampleBufferCopy = new float[sampleBuffer.Length];
            Array.Copy(sampleBuffer, sampleBufferCopy, sampleBuffer.Length);
            sampleBuffer = sampleBufferCopy;
        }

        // Apply amplification
        ApplyAmplification(sampleBuffer, startIndexInclusive, startIndexInclusive + sampleCountToUse, micProfile.AmplificationMultiplier);

        // Check if samples is louder than threshold
        if (!IsAboveNoiseSuppressionThreshold(sampleBuffer, startIndexInclusive, startIndexInclusive + sampleCountToUse, micProfile.NoiseSuppression))
        {
            dywaPitchTracker.ClearPitchHistory();
            return null;
        }

        // Find frequency
        float frequency = dywaPitchTracker.ComputePitch(sampleBuffer, startIndexInclusive, sampleCountToUse);
        if (frequency <= 0)
        {
            dywaPitchTracker.ClearPitchHistory();
            return null;
        }

        int midiNote = GetMidiNoteForFrequency(frequency);
        return new PitchEvent(midiNote);
    }

    private int GetMidiNoteForFrequency(float frequency)
    {
        int bestHalftoneIndex = -1;
        float bestFrequencyDifference = float.MaxValue;
        for (int i = 0; i < halftoneFrequencies.Length; i++)
        {
            float frequencyDifference = Mathf.Abs(halftoneFrequencies[i] - frequency);
            if (frequencyDifference < bestFrequencyDifference)
            {
                bestFrequencyDifference = frequencyDifference;
                bestHalftoneIndex = i;
            }
        }
        return MidiUtils.SingableNoteMin + bestHalftoneIndex;
    }
}
