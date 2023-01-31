using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UnityEngine.SceneManagement;

public class UltraStarPlaySceneInjectionManager : SceneInjectionManager
{
    public static UltraStarPlaySceneInjectionManager Instance => GameObjectUtils.FindComponentWithTag<UltraStarPlaySceneInjectionManager>("SceneInjectionManager");

    protected override GameObject[] GetRootGameObjects(Scene scene)
    {
        return base.GetRootGameObjects(scene)
            .Union(new List<GameObject> { DontDestroyOnLoadManager.Instance.gameObject })
            .ToArray();
    }
}
