using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void MoveNotesHorizontal(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes=null)
    {
        foreach (Note note in selectedNotes.Union(followingNotes ?? new List<Note>()))
        {
            note.MoveHorizontal(distanceInBeats);
        }
    }

    public void MoveNotesVertical(int distanceInMidiNotes, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes=null)
    {
        foreach (Note note in selectedNotes.Union(followingNotes ?? new List<Note>()))
        {
            note.MoveVertical(distanceInMidiNotes);
        }
    }

    public void MoveNotesVerticalAndNotify(int distanceInMidiNotes, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes=null)
    {
        MoveNotesVertical(distanceInMidiNotes, selectedNotes, followingNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }

    public void MoveNotesHorizontalAndNotify(int distanceInBeats, IEnumerable<Note> selectedNotes, IEnumerable<Note> followingNotes=null)
    {
        MoveNotesHorizontal(distanceInBeats, selectedNotes, followingNotes);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}
