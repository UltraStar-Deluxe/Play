using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class EditLyricsUtils
{
    public static readonly string syllableSeparator = ";";
    public static readonly string wordSeparator = " ";
    public static readonly string sentenceSeparator = "\n";
    
    public static bool TryApplyEditModeText(
        SongMeta songMeta,
        Note note,
        string newText,
        out List<Note> notesAfterSplit)
    {
        string viewModeText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);

        if (StringUtils.IsOnlyWhitespace(viewModeText))
        {
            notesAfterSplit = new List<Note> { note };
            return false;
        }

        // Replace multiple control characters with a single character
        viewModeText = Regex.Replace(viewModeText, @"\s+", wordSeparator);
        viewModeText = Regex.Replace(viewModeText, $@"{syllableSeparator}+", syllableSeparator);

        // Split note to apply space and semicolon control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        notesAfterSplit = SplitNoteForNewText(songMeta, note, viewModeText);
        return true;
    }

    private static List<Note> SplitNoteForNewText(
        SongMeta songMeta,
        Note note,
        string newText)
    {
        List<Note> notesAfterSplit = new List<Note> { note };
        if (note.Length <= 1
            || StringUtils.IsOnlyWhitespace(newText))
        {
            return notesAfterSplit;
        }

        List<int> splitIndexes = AllIndexesOfCharacterBeforeTextEnd(newText, ' ')
            .ToList()
            .Union(AllIndexesOfCharacterBeforeTextEnd(newText, ';').ToList())
            .Distinct()
            .ToList();
        splitIndexes.Sort();
        if (splitIndexes.IsNullOrEmpty())
        {
            // Nothing to split
            return notesAfterSplit;
        }

        splitIndexes = splitIndexes
            .Select(index => index + 1)
            .ToList();

        if (!splitIndexes.Contains(newText.Length))
        {
            splitIndexes.Add(newText.Length);
        }

        List<int> splitBeats = splitIndexes
            .Select(index => (int)Math.Floor(note.StartBeat + note.Length * ((double)index / newText.Length)))
            .ToList();

        // Change original note
        note.SetEndBeat(splitBeats[0]);
        note.SetText(newText.Substring(0, splitIndexes[0]));

        int lastSplitIndex = splitIndexes[0];
        int lastSplitBeat = note.EndBeat;

        // Start from 1 because original note was changed already above
        for (int i = 1; i < splitIndexes.Count; i++)
        {
            int splitIndex = splitIndexes[i];
            int splitBeat = splitBeats[i];

            int newNoteStartBeat = lastSplitBeat;
            int newNoteEndBeat = splitBeat;
            int length = splitIndex - lastSplitIndex;
            string newNoteText = newText.Substring(lastSplitIndex, length);

            Note newNote = new(note.Type, newNoteStartBeat, newNoteEndBeat - newNoteStartBeat, note.TxtPitch, newNoteText);
            notesAfterSplit.Add(newNote);
            newNote.SetSentence(note.Sentence);

            lastSplitIndex = splitIndex;
            lastSplitBeat = splitBeat;
        }

        // Remove semicolon from lyrics. These are only used to separate notes in the song editor.
        notesAfterSplit.ForEach(currentNote =>
            currentNote.SetText(currentNote.Text.Replace(";", "")));

        return notesAfterSplit;
    }

    private static List<int> AllIndexesOfCharacterBeforeTextEnd(string text, char searchChar)
    {
        List<int> result = new();
        for (int i = 0; i < text.Length - 1; i++)
        {
            char c = text[i];
            if (c == searchChar)
            {
                result.Add(i);
            }
        }

        return result;
    }
}
