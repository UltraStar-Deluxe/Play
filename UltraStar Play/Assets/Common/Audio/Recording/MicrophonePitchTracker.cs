using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pitch;
using UniRx;
using UnityEngine;
using static Pitch.PitchTracker;

public class MicrophonePitchTracker : AbstractMicPitchTracker
{
    private readonly PitchTracker pitchTracker = new PitchTracker();

    [Range(1, 20)]
    public int pitchRecordHistoryLength = 5;
    private readonly List<PitchRecord> pitchRecordHistory = new List<PitchRecord>();

    [ReadOnly]
    public string lastMidiNoteName;

    private int lastRecordedFrame;

    protected override void EnablePitchTracker()
    {
        pitchTracker.PitchRecordsPerSecond = 30;
        pitchTracker.RecordPitchRecords = true;
        pitchTracker.PitchRecordHistorySize = 5;
        pitchTracker.SampleRate = SampleRate;

        pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(OnPitchDetected);
    }

    protected override void DisablePitchTracker()
    {
        pitchTracker.PitchDetected -= new PitchTracker.PitchDetectedHandler(OnPitchDetected);
    }

    protected override void ProcessMicData(float[] pitchDetectionBuffer, int samplesSinceLastFrame)
    {
        pitchTracker.ProcessBuffer(PitchDetectionBuffer, samplesSinceLastFrame);
    }

    private void OnPitchDetected(PitchTracker sender, PitchRecord pitchRecord)
    {
        // Ignore multiple events in same frame.
        if (lastRecordedFrame == Time.frameCount)
        {
            return;
        }
        lastRecordedFrame = Time.frameCount;

        // Create history of PitchRecord events
        pitchRecordHistory.Add(pitchRecord);
        while (pitchRecordHistoryLength > 0 && pitchRecordHistory.Count > pitchRecordHistoryLength)
        {
            pitchRecordHistory.RemoveAt(0);
        }

        // Calculate median of recorded midi note values.
        // This is done to make the pitch detection more stable, but it increases the latency.
        List<PitchRecord> sortedPitchRecordHistory = new List<PitchRecord>(pitchRecordHistory);
        sortedPitchRecordHistory.Sort((p1, p2) => p1.MidiNote.CompareTo(p2.MidiNote));
        int midiNoteMedian = sortedPitchRecordHistory[sortedPitchRecordHistory.Count / 2].MidiNote;
        pitchEventStream.OnNext(new PitchEvent(midiNoteMedian));

        // Update label in inspector for debugging.
        if (midiNoteMedian > 0)
        {
            lastMidiNoteName = MidiUtils.GetAbsoluteName(midiNoteMedian);
        }
        else
        {
            lastMidiNoteName = "";
        }
    }
}
