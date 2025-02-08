using System;
using System.Collections.Generic;
using System.Linq;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
using UnityEngine;

public static class PitchDetectionUtils
{
    public static async Awaitable<List<Note>> CreateNotesUsingBasicPitchAsync(
        PitchDetectionManager pitchDetectionManager,
        SongMeta songMeta,
        IJob parentJob = null)
    {
        if (!SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            throw new PitchDetectionException("Vocals audio not found. Split the audio first.");
        }

        BasicPitchDetectionResult basicPitchDetectionResult = await pitchDetectionManager.ProcessSongMetaInJobAsync(songMeta, parentJob);
        MidiFile midiFile = MidiFileUtils.LoadMidiFile(basicPitchDetectionResult.MidiFilePath);

        MidiFileUtils.CalculateMidiEventTimesInMillis(
            midiFile,
            out Dictionary<MidiEvent, int> midiEventToDeltaTimeInMillis,
            out Dictionary<MidiEvent, int> midiEventToAbsoluteDeltaTimeInMillis);

        List<Note> loadedNotes = MidiToSongMetaUtils.LoadNotesFromMidiFile(
            songMeta,
            midiFile,
            1,
            0,
            false,
            true,
            midiEventToDeltaTimeInMillis,
            midiEventToAbsoluteDeltaTimeInMillis);
        return loadedNotes;
    }

    public static void MoveNotesToDetectedPitchUsingPitchDetectionLayer(
        SongMeta songMeta,
        List<Note> notes,
        List<Note> pitchDetectionLayerNotes)
    {
        int minBeat = SongMetaUtils.MinBeat(notes);
        int maxBeat = SongMetaUtils.MaxBeat(notes);
        List<Note> pitchDetectionLayerNotesInRange = pitchDetectionLayerNotes
            .Where(it => minBeat <= it.EndBeat && it.StartBeat <= maxBeat)
            .ToList();
        if (pitchDetectionLayerNotesInRange.IsNullOrEmpty())
        {
            return;
        }

        // Map beat to detected pitches
        Dictionary<int, List<int>> beatToDetectedPitches = new();
        foreach (Note pitchDetectionLayerNote in pitchDetectionLayerNotesInRange)
        {
            for (int beat = pitchDetectionLayerNote.StartBeat; beat < pitchDetectionLayerNote.EndBeat; beat++)
            {
                beatToDetectedPitches.AddInsideList(beat, pitchDetectionLayerNote.MidiNote);
            }
        }

        int localAverageWindowSizeInBeats = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, 3000);
        localAverageWindowSizeInBeats = NumberUtils.Limit(localAverageWindowSizeInBeats, 1, int.MaxValue);

        foreach (Note note in notes)
        {
            // Move note to pitch that is closest to local average on pitch detection layer
            List<int> detectedPitchesOfNote = new();
            for (int beat = note.StartBeat; beat < note.EndBeat; beat++)
            {
                if (beatToDetectedPitches.ContainsKey(beat))
                {
                    List<int> detectedPitchesOfBeat = beatToDetectedPitches[beat];
                    if (detectedPitchesOfBeat.Count == 1)
                    {
                        detectedPitchesOfNote.Add(detectedPitchesOfBeat[0]);
                    }
                    else if (detectedPitchesOfBeat.Count > 1)
                    {
                        if (TryFindLocalAveragePitch(pitchDetectionLayerNotes, beat, localAverageWindowSizeInBeats, out int localAveragePitch))
                        {
                            int detectedPitchOfBeatClosestToAverage = detectedPitchesOfBeat.FindMinElement(pitch => Math.Abs(pitch - localAveragePitch));
                            detectedPitchesOfNote.Add(detectedPitchOfBeatClosestToAverage);
                        }
                        else
                        {
                            // Could not determine best candidate. Just take the first one.
                            detectedPitchesOfNote.Add(detectedPitchesOfBeat.FirstOrDefault());
                        }
                    }
                }
            }

            if (!detectedPitchesOfNote.IsNullOrEmpty())
            {
                int medianMidiNote = NumberUtils.Median(detectedPitchesOfNote);
                note.SetMidiNote(medianMidiNote);
            }
        }
    }

    private static bool TryFindLocalAveragePitch(List<Note> notes, int beat, int localAverageWindowSizeInBeats, out int localAveragePitch)
    {
        List<Note> notesInWindow = notes
            .Where(note => note.StartBeat - localAverageWindowSizeInBeats <= beat
                           && beat < note.EndBeat + localAverageWindowSizeInBeats)
            .ToList();
        if (notesInWindow.IsNullOrEmpty())
        {
            localAveragePitch = 0;
            return false;
        }

        localAveragePitch = (int)notesInWindow
            .Select(note => note.MidiNote)
            .Average();
        return true;
    }
}
