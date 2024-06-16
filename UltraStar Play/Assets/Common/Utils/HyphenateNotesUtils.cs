using System.Collections.Generic;
using NHyphenator;

public static class HyphenateNotesUtils
{
    public static Dictionary<Note, List<Note>> HypenateNotes(SongMeta songMeta, List<Note> createdNotes, Hyphenator hyphenator)
    {
        Dictionary<Note, List<Note>> noteToNotesAfterSplit = new();
    
        foreach (Note note in createdNotes)
        {
            string newText = hyphenator.HyphenateText(note.Text);
            if (newText == note.Text)
            {
                continue;
            }

            EditLyricsUtils.TryApplyEditModeText(songMeta, note, newText, out List<Note> notesAfterSplit);
            noteToNotesAfterSplit[note] = notesAfterSplit;
        }

        return noteToNotesAfterSplit;
    }
}
