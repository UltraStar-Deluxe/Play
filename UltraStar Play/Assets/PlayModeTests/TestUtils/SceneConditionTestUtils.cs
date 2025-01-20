using UnityEngine;

public class SceneConditionTestUtils
{
    public static async Awaitable ExpectScene(EScene scene, WaitForConditionConfig config = null)
    {
        await ConditionTestUtils.WaitForCondition(() => SceneNavigator.Instance.CurrentScene == scene, config);
    }
}
