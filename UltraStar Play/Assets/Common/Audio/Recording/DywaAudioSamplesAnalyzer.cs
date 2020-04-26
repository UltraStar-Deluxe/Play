using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CircularBuffer;
using UnityEngine;

public class DywaAudioSamplesAnalyzer : AbstractAudioSamplesAnalyzer
{
    private const int MinSampleLength = 256;
    private readonly int maxSampleLength;

    private DywaPitchTracker dywaPitchTracker;
    private float[] halftoneFrequencies;

    public DywaAudioSamplesAnalyzer(int sampleRateHz, int maxSampleLength)
    {
        this.maxSampleLength = maxSampleLength;
        halftoneFrequencies = MidiUtils.PrecalculateHalftoneFrequencies(MidiUtils.SingableNoteMin, MidiUtils.SingableNoteRange);

        // Create and configure Dynamic Wavelet Pitch Tracker.
        dywaPitchTracker = new DywaPitchTracker();
        dywaPitchTracker.SampleRateHz = sampleRateHz;
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
        int sampleCountToUse = PreviousPowerOfTwo(samplesSinceLastFrame);

        // The number of analyzed samples impacts the performance notably.
        // Do not analyze more samples than necessary.
        if (sampleCountToUse > maxSampleLength)
        {
            sampleCountToUse = PreviousPowerOfTwo(maxSampleLength);
        }

        // Check if samples is louder than threshhold
        if (!IsAboveNoiseSuppressionThreshold(audioSamplesBuffer, sampleCountToUse, micProfile))
        {
            dywaPitchTracker.ClearPitchHistory();
            return null;
        }

        // Find frequency
        float frequency = dywaPitchTracker.ComputePitch(audioSamplesBuffer, 0, sampleCountToUse);
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
