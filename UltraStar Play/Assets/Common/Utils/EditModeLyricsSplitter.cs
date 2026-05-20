using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniInject;

public class EditModeLyricsSplitter : INeedInjection
{
    [Inject]
    private Settings settings;

    private char WordSeparator => settings.SongEditorSettings.WordSeparator;
    private char SyllableSeparator => settings.SongEditorSettings.SyllableSeparator;
    private static readonly char escapeCharacter = '\\';

    /**
     * Split note on space and semicolon characters.
     */
    public bool TryApplyEditModeText(
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
        viewModeText = Regex.Replace(viewModeText, $@"\{WordSeparator.ToString()}+", WordSeparator.ToString());
        viewModeText = Regex.Replace(viewModeText, $@"\{SyllableSeparator.ToString()}+", SyllableSeparator.ToString());

        // Split note to apply space and semicolon control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        notesAfterSplit = SplitNoteForNewText(note, viewModeText);
        return true;
    }

    private List<Note> SplitNoteForNewText(
        Note note,
        string newText)
    {
        List<Note> notesAfterSplit = new List<Note> { note };
        if (note.Length <= 1
            || StringUtils.IsOnlyWhitespace(newText))
        {
            return notesAfterSplit;
        }

        List<int> splitIndexes = AllIndexesOfCharacterBeforeTextEnd(newText, WordSeparator)
            .ToList()
            .Union(AllIndexesOfCharacterBeforeTextEnd(newText, SyllableSeparator).ToList())
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
            currentNote.SetText(currentNote.Text.Replace(SyllableSeparator.ToString(), "")));

        return notesAfterSplit;
    }

    private static List<int> AllIndexesOfCharacterBeforeTextEnd(string text, char searchChar)
    {
        bool foundEscapeCharacter = false;
        List<int> result = new();
        for (int i = 0; i < text.Length - 1; i++)
        {
            if (foundEscapeCharacter)
            {
                foundEscapeCharacter = false;
                continue;
            }
            
            char c = text[i];
            if (c == escapeCharacter)
            {
                foundEscapeCharacter = true;
            }
            else if (c == searchChar)
            {
                result.Add(i);
            }
        }

        return result;
    }
}
