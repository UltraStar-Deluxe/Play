using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PitchDetectionResultMapper
{
    public static List<Note> ToSongMetaNotes(SongMeta songMeta, PitchDetectionResult pitchDetectionResult)
    {
        return pitchDetectionResult.Notes
            .Select(pitchDetectionResultNote => ToSongMetaNote(songMeta, pitchDetectionResultNote))
            .Where(it => it != null)
            .ToList();
    }

    private static Note ToSongMetaNote(SongMeta songMeta, PitchDetectionResultNote pitchDetectionResultNote)
    {
        try
        {
            return new Note(
                ENoteType.Normal,
                (int)SongMetaBpmUtils.MillisToBeats(songMeta, pitchDetectionResultNote.StartInMillis),
                (int)Math.Ceiling(SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, pitchDetectionResultNote.LengthInMillis)),
                MidiUtils.GetUltraStarTxtPitch(pitchDetectionResultNote.MidiNote),
                "");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }
}
