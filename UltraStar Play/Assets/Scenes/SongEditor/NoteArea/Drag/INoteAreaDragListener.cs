public interface INoteAreaDragListener
{
    void OnBeginDrag(NoteAreaDragEvent dragEvent);
    void OnDrag(NoteAreaDragEvent dragEvent);
    void OnEndDrag(NoteAreaDragEvent dragEvent);

    void CancelDrag();
    bool IsCanceled();
}