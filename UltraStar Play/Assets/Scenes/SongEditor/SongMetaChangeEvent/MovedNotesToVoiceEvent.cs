using System.Collections.Generic;

public class MovedNotesToVoiceEvent : ISongMetaChangeEvent
{
    public IReadOnlyCollection<Note> notes;

    public MovedNotesToVoiceEvent(IReadOnlyCollection<Note> notes)
    {
        this.notes = notes;
    }
}