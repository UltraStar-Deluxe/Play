using System;
using System.Collections.Generic;
using System.Linq;

public static class ForcedAlignmentUtils
{
    public static void MoveNotesToForcedAlignmentResult(
        SongMeta songMeta,
        List<Note> sortedNotes,
        ForcedAlignmentResult forcedAlignmentResult,
        int offsetInBeats)
    {
        List<Note> notesWithText = sortedNotes
            .Where(it => !it.Text.Replace("-", "").Replace("~", "").Trim().IsNullOrEmpty())
            .ToList();

        if (forcedAlignmentResult.Words.Count == notesWithText.Count)
        {
            for (int i = 0; i < notesWithText.Count; i++)
            {
                Note note = notesWithText[i];
                WordTimestamp wordTimestamp = forcedAlignmentResult.Words[i];

                double startInMillis = wordTimestamp.StartTime * 1000;
                double endInMillis = wordTimestamp.EndTime * 1000;
                int startBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, startInMillis) + offsetInBeats;
                int endBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, endInMillis) + offsetInBeats;

                note.SetStartAndEndBeat(startBeat, endBeat);
            }
        }
        else
        {
            Log.Warning(() => $"Forced alignment returned {forcedAlignmentResult.Words.Count} words, but {notesWithText.Count} notes with text were selected.");
        }
    }
    
    public static List<Note> CreateNotesFromForcedAlignmentResult(
        ForcedAlignmentResult forcedAlignmentResult,
        SongMeta songMeta,
        Settings settings,
        int offsetInBeats = 0)
    {
        return forcedAlignmentResult.Words
            .Select(wordTimestamp =>
            {
                double startInMillis = wordTimestamp.StartTime * 1000;
                double endInMillis = wordTimestamp.EndTime * 1000;
                int startBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, startInMillis) + offsetInBeats;
                int endBeat = (int)SongMetaBpmUtils.MillisToBeatsWithoutGap(songMeta, endInMillis) + offsetInBeats;
                int lengthInBeats = Math.Max(1, endBeat - startBeat);
                
                string word = wordTimestamp.Word;
                if (!word.IsNullOrEmpty())
                {
                    word = word.Trim() + " ";
                }
                
                return new Note(
                    ENoteType.Normal,
                    startBeat,
                    lengthInBeats,
                    MidiUtils.GetUltraStarTxtPitch(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
                    word);
            })
            .ToList();
    }
    
    public static string GetLyricsFromNotes(IEnumerable<Note> notes)
    {
        return notes
            .OrderBy(it => it.StartBeat)
            .Select(it => it.Text.Replace("-", "").Replace("~", "").Trim())
            .Where(it => !it.IsNullOrEmpty())
            .JoinWith(" ");
    }
}
