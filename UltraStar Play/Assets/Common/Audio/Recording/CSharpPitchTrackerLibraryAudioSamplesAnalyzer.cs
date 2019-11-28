using System;
using System.Collections.Generic;
using Pitch;
using UniRx;
using UnityEngine;
using static Pitch.PitchTracker;

// Uses a C# PitchTracker library for the analysis.
// The library can be found here: https://archive.codeplex.com/?p=pitchtracker
// TODO: The library does not give good results. Use an own implementation for pitch detection.
public class CSharpPitchTrackerLibraryAudioSamplesAnalyzer : IAudioSamplesAnalyzer
{
    private readonly PitchTracker pitchTracker = new PitchTracker();

    private readonly PitchDetectedHandler pitchDetectedHandler;

    private int lastPitchDetectedFrame;
    private readonly List<PitchRecord> pitchRecordHistory = new List<PitchRecord>();
    private readonly int pitchRecordHistoryLength = 5;

    private readonly Subject<PitchEvent> pitchEventStream;

    public CSharpPitchTrackerLibraryAudioSamplesAnalyzer(Subject<PitchEvent> pitchEventStream)
    {
        this.pitchEventStream = pitchEventStream;
        pitchDetectedHandler = new PitchDetectedHandler(OnPitchDetected);
    }

    public void Enable()
    {
        pitchTracker.PitchRecordsPerSecond = 30;
        pitchTracker.RecordPitchRecords = true;
        pitchTracker.PitchRecordHistorySize = 5;
        pitchTracker.SampleRate = MicrophonePitchTracker.SampleRate;

        pitchTracker.PitchDetected += pitchDetectedHandler;
    }

    public void Disable()
    {
        pitchTracker.PitchDetected -= pitchDetectedHandler;
    }

    public void ProcessAudioSamples(float[] audioSamplesBuffer, int samplesSinceLastFrame)
    {
        pitchTracker.ProcessBuffer(audioSamplesBuffer, samplesSinceLastFrame);
    }

    private void OnPitchDetected(PitchTracker sender, PitchRecord pitchRecord)
    {
        // Ignore multiple events in same frame.
        if (lastPitchDetectedFrame == Time.frameCount)
        {
            return;
        }
        lastPitchDetectedFrame = Time.frameCount;

        // Create history of PitchRecord events
        pitchRecordHistory.Add(pitchRecord);
        while (pitchRecordHistoryLength > 0 && pitchRecordHistory.Count > pitchRecordHistoryLength)
        {
            pitchRecordHistory.RemoveAt(0);
        }

        // Calculate median of recorded midi note values.
        // This is done to make the pitch detection more stable, but it increases the latency.
        List<PitchRecord> sortedPitchRecordHistory = new List<PitchRecord>(pitchRecordHistory);
        sortedPitchRecordHistory.Sort((pitchRecord1, pitchRecord2) => pitchRecord1.MidiNote.CompareTo(pitchRecord2.MidiNote));
        int midiNoteMedian = sortedPitchRecordHistory[sortedPitchRecordHistory.Count / 2].MidiNote;

        PitchEvent pitchEvent = new PitchEvent(midiNoteMedian);
        pitchEventStream.OnNext(pitchEvent);
    }
}