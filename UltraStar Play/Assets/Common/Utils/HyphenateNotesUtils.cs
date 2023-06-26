using System.Collections.Generic;
using NHyphenator;

public static class HyphenateNotesUtils
{
    public static void HypenateNotes(SongMeta songMeta, List<Note> createdNotes, Hyphenator hyphenator)
    {
        List<Note> newNotes = new();
        foreach (Note note in createdNotes)
        {
            string newText = hyphenator.HyphenateText(note.Text);
            if (newText == note.Text)
            {
                continue;
            }

            EditLyricsUtils.TryApplyEditModeText(songMeta, note, newText, out List<Note> notesAfterSplit);
            newNotes.AddRange(notesAfterSplit);
            if (note.Sentence != null)
            {
                newNotes.ForEach(newNote => newNote.SetSentence(note.Sentence));
            }
        }

        foreach (Note newNote in newNotes)
        {
            if (!createdNotes.Contains(newNote))
            {
                createdNotes.Add(newNote);
            }
        }
    }
}
