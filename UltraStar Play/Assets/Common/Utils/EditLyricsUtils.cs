using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        StringBuilder stringBuilder = new();
        bool escapeInProgress = false;
        bool wordSeparatorInProgress = false;
        bool syllableSeparatorInProgress = false;
        foreach (char c in viewModeText)
        {
            if (c == '\\' && !escapeInProgress)
            {
                escapeInProgress = true;
                wordSeparatorInProgress = false;
                syllableSeparatorInProgress = false;
                stringBuilder.Append(c);
            }
            else if (c == ' ' && !escapeInProgress && !wordSeparatorInProgress)
            {
                escapeInProgress = false;
                wordSeparatorInProgress = true;
                syllableSeparatorInProgress = false;
                stringBuilder.Append(c);
            }
            else if (c == ';' && !escapeInProgress && !syllableSeparatorInProgress)
            {
                escapeInProgress = false;
                wordSeparatorInProgress = false;
                syllableSeparatorInProgress = true;
                stringBuilder.Append(c);
            }
            else if (c == ' ' && wordSeparatorInProgress)
            {
            }
            else if (c == ';' && syllableSeparatorInProgress)
            {
            }
            else
            {
                escapeInProgress = false;
                wordSeparatorInProgress = false;
                syllableSeparatorInProgress = false;
                stringBuilder.Append(c);
            }
        }
        viewModeText = stringBuilder.ToString();

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
            // Nothing to split. Remove escape characters from lyrics. Unescaped semicolons are also
            // removed as they are only used to separate notes in the song editor.
            foreach (Note currentNote in notesAfterSplit)
            {
                StringBuilder stringBuilder = new();
                bool escapeInProgress = false;
                foreach (char c in newText)
                {
                    if (c == '\\' && !escapeInProgress)
                    {
                        escapeInProgress = true;
                    }
                    else if (c == ';' && !escapeInProgress)
                    {
                        escapeInProgress = false;
                    }
                    else
                    {
                        escapeInProgress = false;
                        stringBuilder.Append(c);
                    }
                }
                currentNote.SetText(stringBuilder.ToString());
            }

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

        // Remove escape characters from lyrics. Unescaped semicolons are also removed as they are
        // only used to separate notes in the song editor.
        foreach (Note currentNote in notesAfterSplit)
        {
            StringBuilder stringBuilder = new();
            bool escapeInProgress = false;
            foreach (char c in currentNote.Text)
            {
                if (c == '\\' && !escapeInProgress)
                {
                    escapeInProgress = true;
                }
                else if (c == ';' && !escapeInProgress)
                {
                    escapeInProgress = false;
                }
                else
                {
                    escapeInProgress = false;
                    stringBuilder.Append(c);
                }
            }
            currentNote.SetText(stringBuilder.ToString());
        }

        return notesAfterSplit;
    }

    // This function can only be used to search for escapable characters like ' ' and ';'.
    private static List<int> AllIndexesOfCharacterBeforeTextEnd(string text, char searchChar)
    {
        List<int> result = new();
        bool escapeInProgress = false;
        // Skip first and last character
        for (int i = 1; i < text.Length - 1; i++)
        {
            char c = text[i];
            if (escapeInProgress)
            {
                escapeInProgress = false;
            }
            else if (c == '\\')
            {
                escapeInProgress = true;
            }
            else if (c == searchChar)
            {
                result.Add(i);
            }
        }

        return result;
    }
}
