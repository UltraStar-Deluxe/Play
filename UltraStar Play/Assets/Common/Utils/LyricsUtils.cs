using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class LyricsUtils
{
    public static readonly char syllableSeparator = ';';
    public static readonly char wordSeparator = ' ';
    public static readonly char sentenceSeparator = '\n';
    public static readonly char escapeCharacter = '\\';

    public static string GetViewModeText(Voice voice)
    {
        StringBuilder stringBuilder = new();
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        foreach (Sentence sentence in sortedSentences)
        {
            List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
            foreach (Note note in sortedNotes)
            {
                stringBuilder.Append(note.Text);
            }
            stringBuilder.Append(sentenceSeparator);
        }
        return stringBuilder.ToString();
    }

    public static string GetEditModeText(Voice voice)
    {
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        return sortedSentences
            .Select(sentence => GetEditModeText(sentence))
            .JoinWith(sentenceSeparator.ToString());
    }

    public static string GetEditModeText(Sentence sentence)
    {
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
        return FormatAsEditable(sortedNotes.Select(note => note.Text));
    }

    public static string GetEditModeText(Note note)
    {
        return FormatAsEditable(new[] { note.Text });
    }

    /// <summary>
    /// Splits the text into sentences and syllables and applies it to the notes of the voice,
    /// padding with empty strings if necessary.
    /// </summary>
    /// <param name="editModeText"></param>
    /// <param name="voice"></param>
    public static void MapEditModeTextToNotes(string editModeText, Voice voice)
    {
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        string[] editModeSentences = editModeText.Split(sentenceSeparator);

        for (int sentenceIndex = 0; sentenceIndex < sortedSentences.Count; sentenceIndex++)
        {
            Sentence sentence = sortedSentences[sentenceIndex];
            string editModeSentence = (sentenceIndex < editModeSentences.Length) ? editModeSentences[sentenceIndex] : "";

            MapEditModeTextToNotes(editModeSentence, sentence);
        }
    }

    /// <summary>
    /// Splits the text into syllables and applies it to the notes of the sentence, padding with
    /// empty strings if necessary.
    /// </summary>
    /// <param name="editModeSentence"></param>
    /// <param name="sentence"></param>
    public static void MapEditModeTextToNotes(string editModeSentence, Sentence sentence)
    {
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
        List<string> syllables = ParseEditable(editModeSentence);

        for (int noteIndex = 0; noteIndex < sortedNotes.Count; noteIndex++)
        {
            Note note = sortedNotes[noteIndex];
            string syllable = (noteIndex < syllables.Count) ? syllables[noteIndex] : "";

            note.SetText(syllable);
        }
    }

    /// <summary>
    /// Splits the text into syllables and tries to apply them to a single note. If there are
    /// multiple syllables, the note is split into multiple parts and the syllables are applied to
    /// them in order.
    /// </summary>
    /// <param name="note"></param>
    /// <param name="newText"></param>
    /// <returns>The list of notes after the split. The first note is always the original.</returns>
    public static List<Note> SplitNoteAndApplyEditModeText(Note note, string newText)
    {
        List<string> syllables = LyricsUtils.ParseEditable(newText);

        // Change original note
        note.SetText(syllables[0]);
        List<Note> notesAfterSplit = new List<Note> { note };

        if (syllables.Count > 1)
        {
            // The note must be split. Splitting positions try to approximate the lengths of the
            // syllables of the new notes.
            int originalNoteLength = note.Length;
            int syllablesTotalLength = Math.Max(1, syllables.Sum(syllable => syllable.Length));

            int firstEndBeat = (int)Math.Floor(note.StartBeat + originalNoteLength * ((double)syllables[0].Length / syllablesTotalLength));
            if (firstEndBeat <= note.StartBeat)
            {
                firstEndBeat = note.StartBeat + 1;
            }
            note.SetEndBeat(firstEndBeat);

            int lastSyllablesCumulativeLength = syllables[0].Length;

            foreach (string syllable in syllables.Skip(1))
            {
                int startBeat = (int)Math.Floor(note.StartBeat + originalNoteLength * ((double)lastSyllablesCumulativeLength / syllablesTotalLength));
                int syllablesCumulativeLength = lastSyllablesCumulativeLength + syllable.Length;
                int endBeat = (int)Math.Floor(note.StartBeat + originalNoteLength * ((double)syllablesCumulativeLength / syllablesTotalLength));
                if (endBeat <= startBeat)
                {
                    endBeat = startBeat + 1;
                }

                Note newNote = new(note.Type, startBeat, endBeat - startBeat, note.TxtPitch, syllable);
                newNote.SetSentence(note.Sentence);
                notesAfterSplit.Add(newNote);

                lastSyllablesCumulativeLength = syllablesCumulativeLength;
            }
        }

        return notesAfterSplit;
    }

    /// <summary>
    /// Formats a list of syllables into an editable lyrics string. Joins the syllables with
    /// syllable separators or word separators based on context. Adds escape sequences for special
    /// characters when necessary. The opposite of ParseEditable.
    /// </summary>
    /// <param name="syllables"></param>
    /// <returns></returns>
    public static string FormatAsEditable(IEnumerable<string> syllables)
    {
        StringBuilder output = new();
        string lastSyllable = null;
        foreach (string syllable in syllables)
        {
            if (lastSyllable != null)
            {
                bool lastSyllableEndedWithWordSeparator = lastSyllable.Length > 0 && lastSyllable[^1] == wordSeparator;
                bool currentSyllableStartsWithWordSeparator = syllable.Length > 0 && syllable[0] == wordSeparator;
                // Neither syllable has a bordering word separator: Add a syllable separator.
                if (!lastSyllableEndedWithWordSeparator && !currentSyllableStartsWithWordSeparator)
                {
                    output.Append(syllableSeparator);
                }
            }
            for (int i = 0; i < syllable.Length; i++)
            {
                char c = syllable[i];

                if (c == escapeCharacter
                    || c == syllableSeparator
                    // Word separators at the beginning and end of the syllable should not be escaped
                    || (c == wordSeparator && i > 0 && i < syllable.Length - 1))
                {
                    output.Append(escapeCharacter);
                    output.Append(c);
                }
                else
                {
                    output.Append(c);
                }
            }
            lastSyllable = syllable;
        }
        return output.ToString();
    }

    /// <summary>
    /// Parses an editable lyrics string into a list of syllables. Splits the text on syllable and
    /// word separators, except when escaped with escape sequences. The opposite of
    /// FormatAsEditable. This can only be used within one sentence, it does not understand sentence separators.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static List<string> ParseEditable(string text)
    {
        List<string> syllables = new();
        StringBuilder output = new();
        bool escapeInProgress = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            // Unescaped escape character: Do not write the escape character, start an escape sequence.
            if (c == escapeCharacter && !escapeInProgress)
            {
                escapeInProgress = true;
            }
            // Unescaped syllable separator: Do not write the syllable separator, start a new syllable.
            else if (c == syllableSeparator && !escapeInProgress)
            {
                syllables.Add(output.ToString());
                output.Clear();
            }
            // Unescaped word separator in the middle: Write the word separator, start a new syllable.
            else if (c == wordSeparator && !escapeInProgress && i > 0 && i < text.Length - 1)
            {
                output.Append(c);

                syllables.Add(output.ToString());
                output.Clear();
            }
            // Escaped character: Write the character, end the escape sequence.
            else if (escapeInProgress)
            {
                output.Append(c);

                escapeInProgress = false;
            }
            // Other character or unescaped word separator at the ends: Write the character.
            else
            {
                output.Append(c);
            }
        }
        // End the last syllable
        syllables.Add(output.ToString());
        return syllables;
    }
}
