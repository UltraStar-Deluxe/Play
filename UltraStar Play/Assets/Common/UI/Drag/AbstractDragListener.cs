public abstract class AbstractDragListener<EVENT> : IDragListener<EVENT>
{
    private bool isCanceled;

    public virtual void OnBeginDrag(EVENT dragEvent)
    {
        isCanceled = false;
    }

    public virtual void OnDrag(EVENT dragEvent)
    {
    }

    public virtual void OnEndDrag(EVENT dragEvent)
    {
    }

    public virtual void CancelDrag()
    {
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }
}
