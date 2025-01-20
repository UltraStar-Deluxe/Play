using UnityEngine;

public class SceneConditionTestUtils
{
    public static async Awaitable ExpectScene(EScene scene)
    {
        await ConditionTestUtils.WaitForCondition(() => SceneNavigator.Instance.CurrentScene == scene);
    }
}
