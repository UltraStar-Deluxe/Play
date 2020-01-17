using System.Collections.Generic;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ExtendNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void ExtendNotesLeft(int distanceInBeats, IEnumerable<Note> selectedNotes)
    {
        foreach (Note note in selectedNotes)
        {
            int newStartBeat = note.StartBeat + distanceInBeats;
            if (newStartBeat < note.EndBeat)
            {
                note.SetStartBeat(newStartBeat);
            }
        }
    }

    public void ExtendNotesRight(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        foreach (Note note in selectedNotes)
        {
            int newEndBeat = note.EndBeat + distanceInBeats;
            if (newEndBeat > note.StartBeat)
            {
                note.SetEndBeat(newEndBeat);
            }
        }
        foreach (Note note in followingNotes)
        {
            note.MoveHorizontal(distanceInBeats);
        }
    }

    public void ExtendNotesLeftAndNotify(int distanceInBeats, IEnumerable<Note> selectedNotes)
    {
        ExtendNotesLeft(distanceInBeats, selectedNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    public void ExtendNotesRightAndNotify(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        ExtendNotesRight(distanceInBeats, selectedNotes, followingNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}