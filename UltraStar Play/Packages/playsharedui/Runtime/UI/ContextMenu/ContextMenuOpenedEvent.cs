public class ContextMenuOpenedEvent
{
    public ContextMenuPopupControl Control { get; private set; }

    public ContextMenuOpenedEvent(ContextMenuPopupControl control)
    {
        Control = control;
    }
}
