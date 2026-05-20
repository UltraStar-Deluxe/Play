using System.Collections.Generic;
using NHyphenator;

public static class HyphenateNotesUtils
{
    public static Dictionary<Note, List<Note>> HypenateNotes(List<Note> createdNotes, Hyphenator hyphenator)
    {
        Dictionary<Note, List<Note>> noteToNotesAfterSplit = new();
    
        foreach (Note note in createdNotes)
        {
            string newText = hyphenator.HyphenateText(note.Text);
            if (newText == note.Text)
            {
                continue;
            }

            List<Note> notesAfterSplit = LyricsUtils.SplitNoteAndApplyEditModeText(note, newText);
            noteToNotesAfterSplit[note] = notesAfterSplit;
        }

        return noteToNotesAfterSplit;
    }
}
