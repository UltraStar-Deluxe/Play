using System.Collections.Generic;
using NHyphenator;
using UniInject;

public class NoteHyphenator : INeedInjection
{
    [Inject]
    private EditModeLyricsSplitter editModeLyricsSplitter;
    
    [Inject]
    private EditModeLyricsConverter editModeLyricsConverter;
    
    public Dictionary<Note, List<Note>> HypenateNotes(List<Note> createdNotes, Hyphenator hyphenator)
    {
        Dictionary<Note, List<Note>> noteToNotesAfterSplit = new();
    
        foreach (Note note in createdNotes)
        {
            string newText = hyphenator.HyphenateText(note.Text);
            if (newText == note.Text)
            {
                continue;
            }

            List<Note> notesAfterSplit = editModeLyricsConverter.SplitNoteAndApplyEditModeText(note, newText);
            noteToNotesAfterSplit[note] = notesAfterSplit;
        }

        return noteToNotesAfterSplit;
    }
}
