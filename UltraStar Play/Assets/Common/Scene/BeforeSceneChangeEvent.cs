public class BeforeSceneChangeEvent
{
    public EScene NextScene { get; private set; }

    public BeforeSceneChangeEvent(EScene nextScene)
    {
        this.NextScene = nextScene;
    }
}
