public class BeforeSceneChangeEvent
{
    public EScene NextScene { get; private set; }
    public SceneData SceneData { get; private set; }

    public BeforeSceneChangeEvent(EScene nextScene, SceneData sceneData)
    {
        NextScene = nextScene;
        SceneData = sceneData;
    }
}
