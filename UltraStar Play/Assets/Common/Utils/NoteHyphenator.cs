using System.Collections.Generic;
using NHyphenator;
using UniInject;

public class NoteHyphenator : INeedInjection
{
    [Inject]
    private LyricsEditor lyricsEditor;
    
    public Dictionary<Note, List<Note>> HypenateNotes(SongMeta songMeta, List<Note> createdNotes, Hyphenator hyphenator)
    {
        Dictionary<Note, List<Note>> noteToNotesAfterSplit = new();
    
        foreach (Note note in createdNotes)
        {
            string newText = hyphenator.HyphenateText(note.Text);
            if (newText == note.Text)
            {
                continue;
            }

            lyricsEditor.TryApplyEditModeText(songMeta, note, newText, out List<Note> notesAfterSplit);
            noteToNotesAfterSplit[note] = notesAfterSplit;
        }

        return noteToNotesAfterSplit;
    }
}
