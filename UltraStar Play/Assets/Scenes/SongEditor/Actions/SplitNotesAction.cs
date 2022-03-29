using System.Collections.Generic;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SplitNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public bool CanExecute(IReadOnlyCollection<Note> selectedNotes)
    {
        return selectedNotes.AnyMatch(it => it.Length > 1);
    }

    public void Execute(IReadOnlyCollection<Note> selectedNotes)
    {
        foreach (Note note in selectedNotes)
        {
            if (note.Length > 1)
            {
                string newNoteText;
                if (note.Text.EndsWith(" "))
                {
                    // Move space from the end of the original note
                    // to the end of the newly created note, to preserve lyrics separation.
                    note.SetText(note.Text.Substring(0, note.Text.Length - 1));
                    newNoteText = "~ ";
                }
                else
                {
                    newNoteText = "~";
                }
                int splitBeat = note.StartBeat + (note.Length / 2);
                Note newNote = new Note(note.Type, splitBeat, note.EndBeat - splitBeat, note.TxtPitch, newNoteText);
                newNote.SetSentence(note.Sentence);
                note.SetEndBeat(splitBeat);
            }
        }
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes)
    {
        Execute(selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesSplitEvent());
    }
}
