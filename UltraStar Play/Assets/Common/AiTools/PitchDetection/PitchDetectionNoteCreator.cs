using System.Collections.Generic;
using UnityEngine;

public class PitchDetectionNoteCreator
{
    private readonly PitchDetectionManager pitchDetectionManager;

    public PitchDetectionNoteCreator(PitchDetectionManager pitchDetectionManager)
    {
        this.pitchDetectionManager = pitchDetectionManager;
    }

    public async Awaitable<List<Note>> CreateNotesUsingAiAsync(SongMeta songMeta)
    {
        if (!SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            throw new PitchDetectionException("Vocals audio not found. Split the audio first.");
        }

        PitchDetectionResult pitchDetectionResult = await pitchDetectionManager.ProcessSongMetaJob(songMeta)
            .GetResultAsync();

        List<Note> notes = PitchDetectionResultMapper.ToSongMetaNotes(songMeta, pitchDetectionResult);
        return notes;
    }
}
