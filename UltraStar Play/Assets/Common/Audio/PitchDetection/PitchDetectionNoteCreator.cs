using System.Collections.Generic;
using AudioSynthesis.Midi;
using AudioSynthesis.Midi.Event;
using UnityEngine;

public class PitchDetectionNoteCreator
{
    private readonly PitchDetectionManager pitchDetectionManager;

    public PitchDetectionNoteCreator(PitchDetectionManager pitchDetectionManager)
    {
        this.pitchDetectionManager = pitchDetectionManager;
    }

    public async Awaitable<List<Note>> CreateNotesUsingBasicPitchAsync(SongMeta songMeta)
    {
        if (!SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            throw new PitchDetectionException("Vocals audio not found. Split the audio first.");
        }

        PitchDetectionResult pitchDetectionResult = await pitchDetectionManager.ProcessSongMetaJob(songMeta)
            .GetResultAsync();
        MidiFile midiFile = MidiFileUtils.LoadMidiFile(pitchDetectionResult.MidiFilePath);

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
}
