internal struct ListDragAndDropArgs : IListDragAndDropArgs
{
    public object target { get; set; }

    public int insertAtIndex { get; set; }

    public DragAndDropPosition dragAndDropPosition { get; set; }

    public IDragAndDropData dragAndDropData { get; set; }
}
