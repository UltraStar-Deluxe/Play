using UnityEngine;

public class SceneConditionTestUtils
{
    public static async Awaitable ExpectScene(EScene scene)
    {
        await ConditionTestUtils.WaitForConditionAsync(() => SceneNavigator.Instance.CurrentScene == scene);
    }
}
