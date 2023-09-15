using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class JukeboxAndSing : IModAction, IOnDisableMod
{
    private List<IDisposable> disposables = new List<IDisposable>();

    [Inject]
    private SceneNavigator sceneNavigator;

    public void Invoke()
    {
        disposables.Add(UltraStarPlaySceneInjectionManager.SceneInjectionFinishedEventStream
            .Subscribe(injector =>
            {
                if (sceneNavigator.CurrentScene == EScene.SingScene)
                {
                    OnSingSceneInjectionFinished(injector);
                }
            }));
    }

    private void OnSingSceneInjectionFinished(Injector injector)
    {
        Debug.Log("JukeboxAndSing - entered sing scene");

        // Wait one frame for scene setup to finish
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => 
        {
            GameObject gameObject = new GameObject();
            JukeboxAndSingControl monoBehaviour = gameObject.AddComponent<JukeboxAndSingControl>();
            monoBehaviour.name = "JukeboxAndSingControl";
            injector.Inject(monoBehaviour);
        }));
    }

    public void OnDisableMod()
    {
        disposables.ForEach(it => it.Dispose());
    }
}