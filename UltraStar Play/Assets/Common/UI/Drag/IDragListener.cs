public interface IDragListener<EVENT>
{
    void OnBeginDrag(EVENT dragEvent);
    void OnDrag(EVENT dragEvent);
    void OnEndDrag(EVENT dragEvent);

    void CancelDrag();
    bool IsCanceled();
}