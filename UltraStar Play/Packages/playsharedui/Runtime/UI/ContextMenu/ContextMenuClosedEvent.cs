public class ContextMenuClosedEvent
{
    public ContextMenuPopupControl Control { get; private set; }

    public ContextMenuClosedEvent(ContextMenuPopupControl control)
    {
        Control = control;
    }
}
