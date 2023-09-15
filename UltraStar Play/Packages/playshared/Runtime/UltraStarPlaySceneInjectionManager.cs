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
    private static Subject<Injector> sceneInjectionFinishedEventStream = new();
    public static IObservable<Injector> SceneInjectionFinishedEventStream => sceneInjectionFinishedEventStream;

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
        FireSceneInjectionFinishedEvent(SceneInjector);
    }

    public static void FireSceneInjectionFinishedEvent(Injector sceneInjector)
    {
        sceneInjectionFinishedEventStream.OnNext(sceneInjector);
    }
}
