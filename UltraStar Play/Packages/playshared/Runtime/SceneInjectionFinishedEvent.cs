using UniInject;

public class SceneInjectionFinishedEvent
{
    public Injector SceneInjector { get; private set; }

    public SceneInjectionFinishedEvent(Injector sceneInjector)
    {
        SceneInjector = sceneInjector;
    }
}
