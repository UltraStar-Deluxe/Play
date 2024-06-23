public class SceneChangedEvent
{
    public EScene NewScene { get; private set; }

    public SceneChangedEvent(EScene newScene)
    {
        NewScene = newScene;
    }
}
