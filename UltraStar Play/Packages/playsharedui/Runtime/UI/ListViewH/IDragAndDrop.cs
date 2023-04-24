internal interface IDragAndDrop
{
    void StartDrag(StartDragArgs args);

    void AcceptDrag();

    void SetVisualMode(DragVisualMode visualMode);

    IDragAndDropData data { get; }
}
