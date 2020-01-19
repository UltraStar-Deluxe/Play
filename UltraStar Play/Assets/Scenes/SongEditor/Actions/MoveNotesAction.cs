using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void MoveNotesHorizontal(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        foreach (Note note in selectedNotes.Union(followingNotes))
        {
            note.MoveHorizontal(distanceInBeats);
        }
    }

    public void MoveNotesVertical(int distanceInMidiNotes, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        foreach (Note note in selectedNotes.Union(followingNotes))
        {
            note.MoveVertical(distanceInMidiNotes);
        }
    }

    public void MoveNotesVerticalAndNotify(int distanceInMidiNotes, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        MoveNotesVertical(distanceInMidiNotes, selectedNotes, followingNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    public void MoveNotesHorizontalAndNotify(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes)
    {
        MoveNotesHorizontal(distanceInBeats, selectedNotes, followingNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}