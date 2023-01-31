using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class LyricsUtils
{
    public static readonly char syllableSeparator = ';';
    public static readonly char sentenceSeparator = '\n';
    public static readonly char spaceCharacter = ' ';
    private static readonly Regex whitespaceRegex = new(@"^\s+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsOnlyWhitespace(string newText)
    {
        return string.IsNullOrEmpty(newText) || whitespaceRegex.IsMatch(newText);
    }

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
        StringBuilder stringBuilder = new();
        Note lastNote = null;

        void ProcessNote(Note note)
        {
            if (lastNote != null
                && lastNote.Sentence == note.Sentence)
            {
                // Add a space when the last note ended or the current note started with a space.
                // Otherwise use the non-whitespace syllableSeparator as end-of-note.
                if (lastNote.Text.EndsWith(spaceCharacter)
                    || note.Text.StartsWith(spaceCharacter))
                {
                    stringBuilder.Append(spaceCharacter);
                }
                else
                {
                    stringBuilder.Append(syllableSeparator);
                }
            }
            stringBuilder.Append(note.Text.Trim());

            lastNote = note;
        }

        List<Note> sortedNotes = sentence.Notes.ToList();
        sortedNotes.Sort(Note.comparerByStartBeat);
        sortedNotes.ForEach(ProcessNote);
        return stringBuilder.ToString();
    }

    public static void MapEditModeTextToNotes(string editModeText, IEnumerable<Sentence> sentences)
    {
        int sentenceIndex = 0;
        int noteIndex = 0;
        List<Sentence> sortedSentences = sentences.ToList();
        sortedSentences.Sort(Sentence.comparerByStartBeat);

        List<Note> sortedNotes = (sentenceIndex < sortedSentences.Count)
            ? SongMetaUtils.GetSortedNotes(sortedSentences[sentenceIndex])
            : new List<Note>();

        StringBuilder stringBuilder = new();

        void ApplyNoteText()
        {
            if (noteIndex < sortedNotes.Count)
            {
                sortedNotes[noteIndex].SetText(stringBuilder.ToString());
            }
            stringBuilder = new StringBuilder();
        }

        void SelectNextSentence()
        {
            ApplyNoteText();

            for (int i = noteIndex + 1; i < sortedNotes.Count; i++)
            {
                sortedNotes[i].SetText("");
            }

            sentenceIndex++;
            noteIndex = 0;

            sortedNotes = (sentenceIndex < sortedSentences.Count)
                    ? SongMetaUtils.GetSortedNotes(sortedSentences[sentenceIndex])
                    : new List<Note>();
        }

        void SelectNextNote()
        {
            ApplyNoteText();

            noteIndex++;
        }

        foreach (char c in editModeText)
        {
            if (c == LyricsUtils.sentenceSeparator)
            {
                SelectNextSentence();
            }
            else if (c == LyricsUtils.syllableSeparator)
            {
                SelectNextNote();
            }
            else if (c == ' ')
            {
                stringBuilder.Append(c);
                SelectNextNote();
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        // Apply remaining text
        if (stringBuilder.Length > 0)
        {
            SelectNextNote();
        }

        // Remove old text of following notes. They did not receive new text.
        for (int s = sentenceIndex; s < sortedSentences.Count; s++)
        {
            sortedNotes = SongMetaUtils.GetSortedNotes(sortedSentences[s]);
            for (int n = noteIndex; n < sortedNotes.Count; n++)
            {
                sortedNotes[n].SetText("");
            }
        }
    }
}
