public class TargetNoteControlCreatedEvent
{
    public TargetNoteControl TargetNoteControl { get; private set; }

    public TargetNoteControlCreatedEvent(TargetNoteControl targetNoteControl)
    {
        TargetNoteControl = targetNoteControl;
    }
}
