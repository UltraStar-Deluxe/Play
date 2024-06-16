using UniInject;

public class SceneEnteredContext
{
    public EScene Scene { get; private set; }
    public SceneData SceneData { get; private set; }
    public Injector SceneInjector { get; private set; }

    public SceneEnteredContext(
        EScene scene,
        SceneData sceneData,
        Injector sceneInjector)
    {
        Scene = scene;
        SceneData = sceneData;
        SceneInjector = sceneInjector;
    }
}
