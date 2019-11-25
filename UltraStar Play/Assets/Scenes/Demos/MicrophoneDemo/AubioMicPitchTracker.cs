using System.Collections.Generic;
using Aubio.NET;
using Aubio.NET.Detection;
using Aubio.NET.Vectors;
using UnityEngine;

public class AubioMicPitchTracker : AbstractMicPitchTracker
{
    private int winSize;
    private int hopSize;
    private FVec inputVec;
    private FVec outputVec;

    // value for aubio_pitch_set_silence
    public float silenceThreasholdInDecibel = -25;
    // value for aubio_pitch_set_tolerance
    public float tolerance = 0.1f;

    public PitchDetection pitchDetectionAlgorithm = PitchDetection.Yin;

    protected override void EnablePitchTracker()
    {
        winSize = 1024;
        hopSize = winSize / 4;
        inputVec = new FVec(hopSize);
        outputVec = new FVec(1);
    }

    protected override void DisablePitchTracker()
    {
        inputVec.Dispose();
        outputVec.Dispose();
        AubioUtils.Cleanup();
    }

    protected override void ProcessMicData(float[] pitchDetectionBuffer, int samplesSinceLastFrame)
    {
        // Process all of the samplesSinceLastFrame hop for hop.
        int doneSamplesCount = 0;
        int hopCount = 0;
        int midiNote = 0;
        List<float> candidates = new List<float>();

        using (AubioPitch aubioPitch = CreateAubioPitch())
        {
            do
            {
                int missingSamplesCount = samplesSinceLastFrame - doneSamplesCount;
                int nextSamplesCount = (missingSamplesCount > hopSize) ? hopSize : missingSamplesCount;
                for (int i = 0; i < nextSamplesCount; i++)
                {
                    inputVec[i] = pitchDetectionBuffer[doneSamplesCount + i];
                }

                aubioPitch.Do(inputVec, outputVec);
                float candidate = outputVec[0];
                float confidence = aubioPitch.Confidence;
                if (confidence > 0.25f)
                {
                    candidates.Add(candidate);
                }
                // Debug.Log(
                //     $"hop {hopCount} done with " +
                //     $"samples = {nextSamplesCount}, " +
                //     $"confidence = {confidence:F6}, " +
                //     $"candidate = {candidate:F6}"
                // );

                doneSamplesCount += nextSamplesCount;
                hopCount++;
            } while (doneSamplesCount < samplesSinceLastFrame);

            if (candidates.Count > 0)
            {
                float candidatesMedian = candidates[candidates.Count / 2];
                midiNote = (int)candidatesMedian;
            }
        }
        pitchEventStream.OnNext(new PitchEvent(midiNote));
    }

    private AubioPitch CreateAubioPitch()
    {
        AubioPitch aubioPitch = new AubioPitch(pitchDetectionAlgorithm, winSize, hopSize, SampleRate);
        aubioPitch.Silence = silenceThreasholdInDecibel;
        aubioPitch.Tolerance = tolerance;
        aubioPitch.Unit = PitchUnit.Midi;
        return aubioPitch;
    }
}
