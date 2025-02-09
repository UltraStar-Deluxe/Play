using UniInject;
using UniRx;
using UnityEngine;

public class JukeboxAndSingSceneMod : ISceneMod
{
    [Inject]
    private JukeboxAndSingModSettings modSettings;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene != EScene.SingScene)
        {
            return;
        }
        Debug.Log("JukeboxAndSingSceneMod - entered sing scene");

        // Wait one frame for scene setup to finish
        AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () => 
        {
            GameObject gameObject = new GameObject();
            JukeboxAndSingControl monoBehaviour = gameObject.AddComponent<JukeboxAndSingControl>();
            monoBehaviour.name = "JukeboxAndSingControl";
            sceneEnteredContext.SceneInjector
              .WithBindingForInstance(modSettings)
              .Inject(monoBehaviour);
        });
    }
}