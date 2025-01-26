using UnityEngine;
using static ConditionUtils;

public class SceneConditionTestUtils
{
    public static async Awaitable ExpectScene(EScene scene, WaitForConditionConfig config = null)
    {
        await WaitForCondition(() => SceneNavigator.Instance.CurrentScene == scene, config);
    }
}
