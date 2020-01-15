namespace UniInject
{
    // Marker interface for scripts that want to be notified when their injection has finished.
    public interface IInjectionFinishedListener
    {
        void OnInjectionFinished();
    }
}