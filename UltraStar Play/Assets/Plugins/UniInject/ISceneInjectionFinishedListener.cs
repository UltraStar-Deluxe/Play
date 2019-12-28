namespace UniInject
{
    // Marker interface for scripts that want to be notified when scene injection has finished.
    public interface ISceneInjectionFinishedListener
    {
        void OnSceneInjectionFinished();
    }
}