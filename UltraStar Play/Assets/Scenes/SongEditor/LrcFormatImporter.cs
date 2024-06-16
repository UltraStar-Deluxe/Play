using System.Collections.Generic;
using System.Linq;
using Opportunity.LrcParser;
using UniInject;
using UnityEngine;

public class LrcFormatImporter : INeedInjection
{
    public Translation GetLrcFormatErrorMessage(string lrcText)
    {
        if (lrcText.IsNullOrEmpty())
        {
            return Translation.Empty;
        }

        IParseResult<Line> parseResult = Lyrics.Parse(lrcText);
        if (!parseResult.Exceptions.IsNullOrEmpty())
        {
            ParseException ex = parseResult.Exceptions.FirstOrDefault();
            return Translation.Get(R.Messages.common_errorWithReason, "reason", ex.Message);
        }
        return Translation.Empty;
    }

    public List<Note> ImportLrcFormat(string lrcText, SongMeta songMeta, Settings settings)
    {
        List<Note> notes = new();

        IParseResult<Line> parseResult = Lyrics.Parse(lrcText);
        if (!parseResult.Exceptions.IsNullOrEmpty())
        {
            Debug.LogError("Failed to parse LRC format");
            parseResult.Exceptions.ForEach(ex => Debug.LogException(ex));
            throw parseResult.Exceptions.FirstOrDefault();
        }

        Debug.Log("Parsed LRC format successfully. Creating notes.");
        double millisPerCharacter = 100;
        double beatsPerCharacter = millisPerCharacter / SongMetaBpmUtils.MillisPerBeat(songMeta);
        Debug.Log("beatsPerCharacter: " + beatsPerCharacter);
        for (int i = 0; i < parseResult.Lyrics.Lines.Count; i++)
        {
            Line line = parseResult.Lyrics.Lines[i];
            Line nextLine = i + 1 < parseResult.Lyrics.Lines.Count
                ? parseResult.Lyrics.Lines[i + 1]
                : null;


            string text = line.Content;
            int midiNote = settings.SongEditorSettings.DefaultPitchForCreatedNotes;
            int currentLineMillis = line.Timestamp.Minute * 60 * 1000
                                        + line.Timestamp.Second * 1000
                                        + line.Timestamp.Millisecond;

            int nextLineMillis = -1;
            if (nextLine != null)
            {
                nextLineMillis = nextLine.Timestamp.Minute * 60 * 1000
                                     + nextLine.Timestamp.Second * 1000
                                     + nextLine.Timestamp.Millisecond;
            }

            int currentLineBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, currentLineMillis);
            int lengthInBeats = 1 + (int)(text.Length * beatsPerCharacter);

            // Limit length of note
            if (nextLine != null)
            {
                int nextLineBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, nextLineMillis);
                int maxLengthInBeats = nextLineBeat - currentLineBeat;
                if (lengthInBeats > maxLengthInBeats)
                {
                    lengthInBeats = maxLengthInBeats;
                }
            }

            Note note = new Note(ENoteType.Normal, currentLineBeat, lengthInBeats, MidiUtils.GetUltraStarTxtPitch(midiNote), text);

            // Split note on space and semicolon characters
            EditLyricsUtils.TryApplyEditModeText(songMeta, note, note.Text, out List<Note> notesAfterSplit);

            notes.AddRange(notesAfterSplit);
        }

        SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(notes, settings.SongEditorSettings.SpaceBetweenNotesInMillis, songMeta);

        return notes;
    }
}
