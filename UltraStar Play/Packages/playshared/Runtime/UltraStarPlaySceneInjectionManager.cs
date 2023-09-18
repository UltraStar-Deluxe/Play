using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UltraStarPlaySceneInjectionManager : SceneInjectionManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        sceneInjectionFinishedEventStream = new();
    }
    private static Subject<SceneInjectionFinishedEvent> sceneInjectionFinishedEventStream = new();
    public static IObservable<SceneInjectionFinishedEvent> SceneInjectionFinishedEventStream => sceneInjectionFinishedEventStream;

    public static UltraStarPlaySceneInjectionManager Instance => GameObjectUtils.FindComponentWithTag<UltraStarPlaySceneInjectionManager>("SceneInjectionManager");

    protected override GameObject[] GetRootGameObjects(Scene scene)
    {
        return base.GetRootGameObjects(scene)
            .Union(new List<GameObject> { DontDestroyOnLoadManager.Instance.gameObject })
            .ToArray();
    }

    public override void DoSceneInjection()
    {
        base.DoSceneInjection();
        FireSceneInjectionFinishedEvent(new SceneInjectionFinishedEvent(SceneInjector));
    }

    public static void FireSceneInjectionFinishedEvent(SceneInjectionFinishedEvent evt)
    {
        sceneInjectionFinishedEventStream.OnNext(evt);
    }
}
