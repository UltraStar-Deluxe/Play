using UniRx;

public class CancelableEvent
{
    public Translation cancelMessage;

    public static bool IsCanceledByEvent(Subject<CancelableEvent> eventStream, bool showNotification = true)
    {
        CancelableEvent evt = new();
        eventStream.OnNext(evt);
        bool isCanceled = !evt.cancelMessage.Value.IsNullOrEmpty();
        if (isCanceled
            && showNotification)
        {
            NotificationManager.CreateNotification(evt.cancelMessage);
        }

        return isCanceled;
    }
}
