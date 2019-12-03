using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CamdAudioSamplesAnalyzer : IAudioSamplesAnalyzer
{
    /** There are 49 halftones in the hearable audio spectrum (C2 to C6 (1046.5023 Hz)). */
    private const int NumHalftones = 49;
    /** A4 concert pitch of 440 Hz. */
    private const float BaseToneFreq = 440f;
    private const int BaseToneMidi = 33;
    private const int MinSampleLength = 256;
    private static readonly double[] halftoneFrequencies = PrecalculateHalftoneFrequencies();

    private readonly int[] halftoneDelays;
    private readonly Subject<PitchEvent> pitchEventStream;
    private readonly List<int> pitchRecordHistory = new List<int>();
    private readonly int pitchRecordHistoryLength = 5;

    private bool isEnabled;
    private int lastPitchDetectedFrame;

    public CamdAudioSamplesAnalyzer(Subject<PitchEvent> pitchEventStream, int sampleRateHz)
    {
        this.pitchEventStream = pitchEventStream;
        halftoneDelays = PrecalculateHalftoneDelays(halftoneFrequencies, sampleRateHz);
    }

    private static double[] PrecalculateHalftoneFrequencies()
    {
        double[] noteFrequencies = new double[NumHalftones];
        for (int index = 0; index < NumHalftones; index++)
        {
            noteFrequencies[index] = BaseToneFreq * Math.Pow(2f, (index - BaseToneMidi) / 12f);
        }
        return noteFrequencies;
    }

    private static int[] PrecalculateHalftoneDelays(double[] halftoneFrequencies, double sampleRateHz)
    {
        int[] noteDelays = new int[NumHalftones];
        for (int index = 0; index < NumHalftones; index++)
        {
            noteDelays[index] = Convert.ToInt32((sampleRateHz) / halftoneFrequencies[index]);
        }
        return noteDelays;
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }

    public void ProcessAudioSamples(float[] audioSamplesBuffer, int samplesSinceLastFrame)
    {
        if (!isEnabled || samplesSinceLastFrame < MinSampleLength)
        {
            return;
        }
        int sampleCountToUse = PreviousPowerOfTwo(samplesSinceLastFrame);

        // check if samples is louder than threshhold
        bool passesThreshhold = false;
        for (int index = 0; index < sampleCountToUse; index++)
        {
            if (Math.Abs(audioSamplesBuffer[index]) >= 0.05f)
            {
                passesThreshhold = true;
                break;
            }
        }
        if (!passesThreshhold)
        {
            // Subscribers (such as the PlayerNoteRecorder) must know that the singing ended.
            // Therefore, a midi pitch of 0 is interpreted as "no singing".
            OnPitchDetected(0);
        }

        // get best fitting tone
        double[] correlation = CircularAverageMagnitudeDifference(audioSamplesBuffer, sampleCountToUse);

        // no idea where the +3 is coming from...
        int halftone = CalculateBestFittingHalftone(correlation) + BaseToneMidi + 3;
        if (halftone != -1 && isEnabled)
        {
            OnPitchDetected(halftone);
        }
        // else: no tone detected.
    }

    private static int PreviousPowerOfTwo(int x)
    {
        x |= (x >> 1);
        x |= (x >> 2);
        x |= (x >> 4);
        x |= (x >> 8);
        x |= (x >> 16);
        return x - (x >> 1);
    }

    private static int CalculateBestFittingHalftone(double[] correlation)
    {
        if (correlation.Length == 0)
        {
            return -1;
        }
        int bestFittingHalftone = 0;
        for (int index = 1; index < NumHalftones; index++)
        {
            if (correlation[index] <= correlation[bestFittingHalftone])
            {
                bestFittingHalftone = index;
            }
        }
        return bestFittingHalftone;
    }

    // Circular Average Magnitude Difference Function (CAMDF) is defined as
    //   D_C(\tau)=\sum_{n=0}^{N-1}|x(mod(n+\tau, N)) - x(n)|
    // where \tau = halftoneDelay, n = index, N = samplesSinceLastFrame, x = audioSamplesBuffer
    // See: Equation (4) in http://www.utdallas.edu/~hxb076000/citing_papers/Muhammad%20Extended%20Average%20Magnitude%20Difference.pdf
    private double[] CircularAverageMagnitudeDifference(float[] audioSamplesBuffer, int samplesSinceLastFrame)
    {
        double[] correlation = new double[NumHalftones];
        // accumulate the magnitude differences for samples in AnalysisBuffer
        for (int halftone = 0; halftone < NumHalftones; halftone++)
        {
            correlation[halftone] = 0;
            for (int index = 0; index < samplesSinceLastFrame; index++)
            {
                correlation[halftone] = correlation[halftone] +
                    Math.Abs(
                        audioSamplesBuffer[(index + halftoneDelays[halftone]) & (samplesSinceLastFrame - 1)] -
                        audioSamplesBuffer[index]);
            }
            correlation[halftone] = correlation[halftone] / samplesSinceLastFrame;
        }
        // return circular average magnitude difference
        return correlation;
    }

    private void OnPitchDetected(int midiPitch)
    {
        // Ignore multiple events in same frame.
        if (lastPitchDetectedFrame == Time.frameCount)
        {
            return;
        }
        lastPitchDetectedFrame = Time.frameCount;

        // Create history of PitchRecord events
        pitchRecordHistory.Add(midiPitch);
        while (pitchRecordHistoryLength > 0 && pitchRecordHistory.Count > pitchRecordHistoryLength)
        {
            pitchRecordHistory.RemoveAt(0);
        }

        // Calculate median of recorded midi note values.
        // This is done to make the pitch detection more stable, but it increases the latency.
        List<int> sortedPitchRecordHistory = new List<int>(pitchRecordHistory);
        sortedPitchRecordHistory.Sort((pitchRecord1, pitchRecord2) => pitchRecord1.CompareTo(pitchRecord2));
        int midiNoteMedian = sortedPitchRecordHistory[sortedPitchRecordHistory.Count / 2];

        PitchEvent pitchEvent = new PitchEvent(midiNoteMedian);
        pitchEventStream.OnNext(pitchEvent);
    }
}