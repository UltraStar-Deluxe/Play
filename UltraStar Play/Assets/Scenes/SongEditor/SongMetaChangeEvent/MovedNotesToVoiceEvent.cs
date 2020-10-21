using System.Collections.Generic;

public class MovedNotesToVoiceEvent : SongMetaChangeEvent
{
    public IReadOnlyCollection<Note> notes;

    public MovedNotesToVoiceEvent(IReadOnlyCollection<Note> notes)
    {
        this.notes = notes;
    }
}