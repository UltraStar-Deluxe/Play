using UnityEngine;
using static ConditionUtils;

public class SceneConditionTestUtils
{
    public static async Awaitable ExpectSceneAsync(EScene scene, WaitForConditionConfig config = null)
    {
        await WaitForConditionAsync(() => SceneNavigator.Instance.CurrentScene == scene, config);
    }
}
