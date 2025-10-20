using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniInject;

public class EditModeLyricsConverter : INeedInjection
{
    [Inject]
    private Settings settings;

    private char WordSeparator => settings.SongEditorSettings.WordSeparator;
    private char SyllableSeparator => settings.SongEditorSettings.SyllableSeparator;
    private char SentenceSeparator => settings.SongEditorSettings.SentenceSeparator;

    public string GetViewModeText(Voice voice)
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
            stringBuilder.Append(SentenceSeparator);
        }
        return stringBuilder.ToString();
    }

    public string GetEditModeText(Voice voice)
    {
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(voice);
        return sortedSentences
            .Select(sentence => GetEditModeText(sentence))
            .JoinWith(SentenceSeparator.ToString());
    }

    public string GetEditModeText(Sentence sentence)
    {
        StringBuilder stringBuilder = new();
        Note lastNote = null;

        void ProcessNote(Note note)
        {
            if (lastNote != null
                && lastNote.Sentence == note.Sentence)
            {
                // Detect border of words, i.e., the last note ended or the current note starts with a space.
                if (lastNote.Text.EndsWith(" ") || note.Text.StartsWith(" "))
                {
                    stringBuilder.Append(WordSeparator);
                }
                else
                {
                    stringBuilder.Append(SyllableSeparator);
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

    public void MapEditModeTextToNotes(string editModeText, IEnumerable<Sentence> sentences)
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
            if (c == SentenceSeparator)
            {
                SelectNextSentence();
            }
            else if (c == SyllableSeparator)
            {
                SelectNextNote();
            }
            else if (c == WordSeparator)
            {
                stringBuilder.Append(' ');
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
