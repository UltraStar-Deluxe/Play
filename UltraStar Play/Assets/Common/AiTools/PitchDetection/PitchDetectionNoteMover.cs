using System;
using System.Collections.Generic;
using System.Linq;

public static class PitchDetectionNoteMover
{
    public static void MoveNotesToDetectedPitch(
        SongMeta songMeta,
        List<Note> notes,
        PitchDetectionResult pitchDetectionResult)
    {
        notes.ForEach(note => MoveNoteToDetectedPitch(songMeta, note, pitchDetectionResult));
    }

    private static void MoveNoteToDetectedPitch(
        SongMeta songMeta,
        Note note,
        PitchDetectionResult pitchDetectionResult)
    {
        if (pitchDetectionResult == null
            || pitchDetectionResult.Notes.IsNullOrEmpty())
        {
            return;
        }
        
        double startInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat);
        double endInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.EndBeat);

        // Find pitch detection results that overlap with the note's time range
        List<PitchDetectionResultNote> overlappingResults = pitchDetectionResult.Notes
            .Where(resultNote => resultNote.StartInMillis < endInMillis
                                 && resultNote.StartInMillis + resultNote.LengthInMillis > startInMillis)
            .ToList();

        if (overlappingResults.IsNullOrEmpty())
        {
            return;
        }

        // Determine the best fitting midi note.
        // We use the MidiNote that has the largest total overlap duration.
        // This is necessary because the same MidiNote might be split across multiple pitchDetectionResult notes.
        Dictionary<int, double> midiNoteToOverlapDuration = new();
        foreach (PitchDetectionResultNote resultNote in overlappingResults)
        {
            double overlapStart = Math.Max(startInMillis, resultNote.StartInMillis);
            double overlapEnd = Math.Min(endInMillis, resultNote.StartInMillis + resultNote.LengthInMillis);
            double overlapDuration = overlapEnd - overlapStart;

            if (overlapDuration > 0)
            {
                if (!midiNoteToOverlapDuration.TryAdd(resultNote.MidiNote, overlapDuration))
                {
                    midiNoteToOverlapDuration[resultNote.MidiNote] += overlapDuration;
                }
            }
        }

        if (midiNoteToOverlapDuration.Count > 0)
        {
            int bestMidiNote = midiNoteToOverlapDuration
                .OrderByDescending(pair => pair.Value)
                .First()
                .Key;
            note.SetMidiNote(bestMidiNote);
        }
    }
}
