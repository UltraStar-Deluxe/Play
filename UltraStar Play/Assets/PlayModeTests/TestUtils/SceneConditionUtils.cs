using UnityEngine;

public class SceneConditionUtils
{
    public static async Awaitable ExpectScene(EScene scene)
    {
        await AwaitableTestUtils.WaitForConditionAsync(() => SceneNavigator.Instance.CurrentScene == scene);
    }
}
