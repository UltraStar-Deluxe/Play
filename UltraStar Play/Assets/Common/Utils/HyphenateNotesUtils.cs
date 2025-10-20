using System.Collections.Generic;
using NHyphenator;
using UniInject;

// TODO: Not a static utils class anymore
public class HyphenateNotesUtils : INeedInjection
{
    [Inject]
    private EditLyricsUtils editLyricsUtils;
    
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

            editLyricsUtils.TryApplyEditModeText(songMeta, note, newText, out List<Note> notesAfterSplit);
            noteToNotesAfterSplit[note] = notesAfterSplit;
        }

        return noteToNotesAfterSplit;
    }
}
