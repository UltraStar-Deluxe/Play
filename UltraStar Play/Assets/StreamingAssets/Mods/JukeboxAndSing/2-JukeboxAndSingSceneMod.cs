using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class JukeboxAndSingSceneMod : ISceneMod
{
    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene != EScene.SingScene)
        {
            return;
        }
        Debug.Log("JukeboxAndSingSceneMod - entered sing scene");

        // Wait one frame for scene setup to finish
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => 
        {
            GameObject gameObject = new GameObject();
            JukeboxAndSingControl monoBehaviour = gameObject.AddComponent<JukeboxAndSingControl>();
            monoBehaviour.name = "JukeboxAndSingControl";
            sceneEnteredContext.SceneInjector.Inject(monoBehaviour);
        }));
    }
}