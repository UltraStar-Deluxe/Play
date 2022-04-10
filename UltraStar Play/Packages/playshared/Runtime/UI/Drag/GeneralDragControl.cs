

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GeneralDragControl : AbstractDragControl<GeneralDragEvent>
{
    protected override GeneralDragEvent CreateDragEventStart(DragControlPointerEvent eventData)
    {
        return CreateGeneralDragEventStart(eventData);
    }

    protected override GeneralDragEvent CreateDragEvent(DragControlPointerEvent eventData, GeneralDragEvent dragStartEvent)
    {
        return CreateGeneralDragEvent(eventData, dragStartEvent);
    }
}
