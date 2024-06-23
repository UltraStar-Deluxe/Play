public class RecordedNoteControlCreatedEvent
{
    public RecordedNoteControl RecordedNoteControl { get; private set; }

    public RecordedNoteControlCreatedEvent(RecordedNoteControl recordedNoteControl)
    {
        RecordedNoteControl = recordedNoteControl;
    }
}
