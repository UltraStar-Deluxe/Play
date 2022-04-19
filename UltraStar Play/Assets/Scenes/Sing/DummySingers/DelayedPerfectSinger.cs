using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DelayedPerfectSinger : PerfectSinger
{
    private readonly List<BeatPitchEvent> delayedPitchEvents = new();

    private int lastAnalyzedBeat = -1;

    protected override void Update()
    {
        if (playerControl == null)
        {
            if (!TryFindPlayerControl())
            {
                return;
            }
        }

        int currentBeat = (int)singSceneControl.CurrentBeat;
        if (currentBeat <= 0
            || currentBeat <= lastAnalyzedBeat
            || playerControl.PlayerMicPitchTracker.RecordingSentence == null)
        {
            return;
        }

        int beatToAnalyze = lastAnalyzedBeat + 1;
        for (int beat = beatToAnalyze; beat <= currentBeat; beat++)
        {
            BeatPitchEvent pitchEvent = GetDummyPitchEvent(beat);
            delayedPitchEvents.Add(pitchEvent);
            lastAnalyzedBeat = beat;
        }

        if (delayedPitchEvents.Count < Random.Range(1, 100))
        {
            return;
        }

        List<BeatPitchEvent> pitchEvents = new(delayedPitchEvents);
        Debug.Log($"Fire {pitchEvents.Count} PitchEvents");
        pitchEvents.ForEach(pitchEvent => FirePitchEvent(pitchEvent, beatToAnalyze));
        delayedPitchEvents.Clear();
    }

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        Note noteAtBeat = GetNoteAtBeat(beat);
        int midiNote = noteAtBeat != null
            ? noteAtBeat.MidiNote
            : 60;
        return new BeatPitchEvent(midiNote + offset, beat);
    }
}
