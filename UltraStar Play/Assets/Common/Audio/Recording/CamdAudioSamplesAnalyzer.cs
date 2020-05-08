using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CircularBuffer;
using UnityEngine;

public class CamdAudioSamplesAnalyzer : AbstractAudioSamplesAnalyzer
{
    // The number of halftones is SingableNoteRange + 1 octave.
    // The additional octave are outside the singable note range.
    // It can be interpreted as a failed pitch detection.
    private const int NumHalftones = MidiUtils.SingableNoteRange + 12;
    private const int MinNote = MidiUtils.SingableNoteMin - 6;
    private const int MinSampleLength = 256;
    // For performance, do not analyze more than necessary.
    private readonly int maxSampleLength;

    private readonly int[] halftoneDelays;
    private readonly float[] correlation = new float[NumHalftones];

    private readonly CamdPitchCandidate[] currentCandidates = new CamdPitchCandidate[3];
    private readonly CircularBuffer<CamdPitchCandidate> candidateHistory = new CircularBuffer<CamdPitchCandidate>(2);

    // The best candidate from the currentCandidates and candidateHistory
    private CamdPitchCandidate bestCandidate;
    // The best candidate from the currentCandidates only
    private CamdPitchCandidate bestCurrentCandidate;

    // Factor in range 0 to 1, where 0 means "no bias" and 1 means "full bias".
    // The factor is used to reduce the normalizedError of a current candidate
    // when there is a candadite with the same halftone in the history.
    // This creates a tendency towards previously detected pitches.
    public float HalftoneContinuationBias { get; set; }

    public CamdAudioSamplesAnalyzer(int sampleRateHz, int maxSampleLength)
    {
        this.maxSampleLength = maxSampleLength;
        float[] halftoneFrequencies = MidiUtils.PrecalculateHalftoneFrequencies(MinNote, NumHalftones);
        halftoneDelays = MidiUtils.PrecalculateHalftoneDelays(sampleRateHz, halftoneFrequencies);
        for (int i = 0; i < currentCandidates.Length; i++)
        {
            currentCandidates[i] = new CamdPitchCandidate();
        }
    }

    public override PitchEvent ProcessAudioSamples(float[] sampleBuffer, int sampleStartIndex, int sampleEndIndex, MicProfile micProfile)
    {
        if (!isEnabled)
        {
            Debug.LogWarning("AudioSamplesAnalyzer is disabled");
            return null;
        }

        int sampleLength = sampleEndIndex - sampleStartIndex;
        if (sampleLength < MinSampleLength)
        {
            return null;
        }
        int sampleCountToUse = PreviousPowerOfTwo(sampleLength);

        // The number of analyzed samples impacts the performance notable.
        // Do not analyze more samples than necessary.
        if (sampleCountToUse > maxSampleLength)
        {
            sampleCountToUse = PreviousPowerOfTwo(maxSampleLength);
        }

        // Check if samples is louder than threshhold
        if (!IsAboveNoiseSuppressionThreshold(sampleBuffer, sampleStartIndex, sampleStartIndex + sampleCountToUse, micProfile))
        {
            OnNoPitchDetected();
            return null;
        }

        // Get best fitting tone
        CircularAverageMagnitudeDifference(sampleBuffer, sampleStartIndex, sampleCountToUse, correlation);

        FindCurrentCandidates(correlation, currentCandidates);
        CalculateNormalizedError(currentCandidates, candidateHistory);
        FindBestCandidate(currentCandidates, candidateHistory);

        int midiNote = bestCandidate.halftone + MinNote;
        if (midiNote < MidiUtils.SingableNoteMin || midiNote > MidiUtils.SingableNoteMax)
        {
            // This pitch is impossible to sing.
            // Thus, assume the pitch detection failed and do not add the pitch to the history.
            return new PitchEvent(midiNote);
        }
        else
        {
            // Fill history with best of the current candidates.
            // But do not re-insert candidates from the history back to the history.
            // Otherwise a candidate with 0 error could dominate forever.
            int bestCurrentCandidateMidiNote = bestCurrentCandidate.halftone + MinNote;
            if (MidiUtils.SingableNoteMin < bestCurrentCandidateMidiNote
                && bestCurrentCandidateMidiNote < MidiUtils.SingableNoteMax)
            {
                candidateHistory.PushFront(new CamdPitchCandidate(bestCurrentCandidate));
            }

            return new PitchEvent(midiNote);
        }
    }

    private void FindBestCandidate(CamdPitchCandidate[] currentCandidates, CircularBuffer<CamdPitchCandidate> bestCandidateHistory)
    {
        bestCurrentCandidate = null;
        currentCandidates.ForEach((currentCandidate) =>
        {
            // Bias towards recently detected pitches.
            // For candidates with similar normalizedError,
            // this will prefer a candidate that has been detected before, which reduces the "jitter" of the pitch detection.
            // However, if a new pitch has been clearly identified with significantly less normalizedError,
            // then an old detected pitch from the history will not be used instead because its normalizedError will still be less - even without bias.
            currentCandidate.normalizedErrorWithBias = currentCandidate.normalizedError;
            if (HalftoneContinuationBias >= 0 && HalftoneContinuationBias <= 1)
            {
                for (int historyIndex = 0; historyIndex < bestCandidateHistory.Size; historyIndex++)
                {
                    if (bestCandidateHistory[historyIndex].halftone == currentCandidate.halftone)
                    {
                        // The further away in the history the halftone was before, the less the bias will impact the current candidate.
                        float biasConsideringHistoryDistance = HalftoneContinuationBias - (HalftoneContinuationBias * (historyIndex / bestCandidateHistory.Size));
                        currentCandidate.normalizedErrorWithBias = currentCandidate.normalizedError * (1 - biasConsideringHistoryDistance);
                        // Do not apply bias multiple times.
                        break;
                    }
                }
            }

            if (currentCandidate.halftone >= 0
                && (bestCurrentCandidate == null
                    || bestCurrentCandidate.halftone < 0
                    || currentCandidate.normalizedErrorWithBias < bestCurrentCandidate.normalizedErrorWithBias))
            {
                bestCurrentCandidate = currentCandidate;
            }
        });

        CamdPitchCandidate bestHistoryCandidate = null;
        for (int i = 0; i < bestCandidateHistory.Size; i++)
        {
            // Do not consider the biased error when comparing best candidate from history.
            CamdPitchCandidate historyCandidate = bestCandidateHistory[i];
            if (historyCandidate.halftone >= 0
                && (bestHistoryCandidate == null
                    || bestHistoryCandidate.halftone < 0
                    || historyCandidate.normalizedError < bestHistoryCandidate.normalizedError))
            {
                bestHistoryCandidate = historyCandidate;
            }
        }

        // Do not consider the biased error when comparing best candidate from history.
        bestCandidate = (bestHistoryCandidate == null || bestCurrentCandidate.normalizedErrorWithBias < bestHistoryCandidate.normalizedError)
            ? bestCurrentCandidate
            : bestHistoryCandidate;
    }

    private void CalculateNormalizedError(CamdPitchCandidate[] currentCandidates, CircularBuffer<CamdPitchCandidate> bestCandidateHistory)
    {
        // Correlation is the error. For a perfect match of the frequency it would be 0.
        // NormalizedError is the percentage of the error for this candidate
        // compared to other current candidates as well as history candidates.
        // Thus, normalizedError is smaller if the candidate has a significantly smaller error than the rest,
        // and it is relatively bigger when all candidates have similar error values.
        float errorSum = 0;
        currentCandidates.ForEach(candidate => errorSum += candidate.error);
        bestCandidateHistory.ForEach(candidate => errorSum += candidate.error);

        currentCandidates.ForEach(candidate => candidate.normalizedError = candidate.error / errorSum);
        bestCandidateHistory.ForEach(candidate => candidate.normalizedError = candidate.error / errorSum);
    }

    private void FindCurrentCandidates(float[] correlation, CamdPitchCandidate[] candidates)
    {
        // Find the halftones with the least error.
        for (int i = 0; i < candidates.Length; i++)
        {
            candidates[i].halftone = 0;
            candidates[i].error = correlation[0];
        }

        for (int halftone = 1; halftone < NumHalftones; halftone++)
        {
            // Index 0: first best candidate, index 1: second best candidate, ...
            for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
            {
                if (candidates[candidateIndex].error > correlation[halftone])
                {
                    candidates[candidateIndex].halftone = halftone;
                    candidates[candidateIndex].error = correlation[halftone];
                    break;
                }
            }
        }
    }

    // Circular Average Magnitude Difference Function (CAMDF) is defined as
    //   D_C(\tau)=\sum_{n=0}^{N-1}|x(mod(n+\tau, N)) - x(n)|
    // where \tau = halftoneDelay, n = index, N = samplesSinceLastFrame, x = sampleCountToUse
    // See: Equation (4) in http://www.utdallas.edu/~hxb076000/citing_papers/Muhammad%20Extended%20Average%20Magnitude%20Difference.pdf
    private void CircularAverageMagnitudeDifference(float[] sampleBuffer, int sampleStartIndex, int sampleCountToUse, float[] correlation)
    {
        // accumulate the magnitude differences for samples in AnalysisBuffer
        for (int halftone = 0; halftone < NumHalftones; halftone++)
        {
            correlation[halftone] = 0;
            for (int i = 0; i < sampleCountToUse; i++)
            {
                // It should only consider the indices from sampleStartIndex to sampleStartIndex + sampleCountToUse |--->----<---|
                // This is achieved via a modulo operation.
                // Binary & is used for efficient calculation of modulo (works because sampleCountToUse is a power of 2).
                float diff = sampleBuffer[sampleStartIndex + ((i + halftoneDelays[halftone]) & (sampleCountToUse - 1))] - sampleBuffer[sampleStartIndex + i];
                correlation[halftone] += (diff < 0) ? -diff : diff;
            }
        }
    }

    private void OnNoPitchDetected()
    {
        candidateHistory.Clear();
    }

    private class CamdPitchCandidate
    {
        public int halftone = -1;
        public float error = -1;
        public float normalizedError = -1;
        public float normalizedErrorWithBias = -1;

        public CamdPitchCandidate()
        {
        }

        public CamdPitchCandidate(CamdPitchCandidate other)
        {
            CopyValues(other);
        }

        public void Reset()
        {
            halftone = -1;
            error = -1;
            normalizedError = -1;
            normalizedErrorWithBias = -1;
        }

        public void CopyValues(CamdPitchCandidate other)
        {
            halftone = other.halftone;
            error = other.error;
            normalizedError = other.normalizedError;
            normalizedErrorWithBias = other.normalizedErrorWithBias;
        }

        public override string ToString()
        {
            return $"(halftone:{halftone};error:{error};normalizedError:{normalizedError})";
        }
    }
}
