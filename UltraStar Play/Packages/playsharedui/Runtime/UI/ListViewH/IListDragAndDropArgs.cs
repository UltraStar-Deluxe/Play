public interface IListDragAndDropArgs
{
    object target { get; }

    int insertAtIndex { get; }

    IDragAndDropData dragAndDropData { get; }

    DragAndDropPosition dragAndDropPosition { get; }
}
