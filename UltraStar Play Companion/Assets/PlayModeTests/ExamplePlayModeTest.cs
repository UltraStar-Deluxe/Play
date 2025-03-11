using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ExamplePlayModeTest
{
    [UnityTest]
    public IEnumerator ShouldRun() => ShouldRunAsync();
    private async Awaitable ShouldRunAsync()
    {
        LogAssert.ignoreFailingMessages = true;
        Debug.Log("Loading MainScene");
        await SceneManager.LoadSceneAsync("MainScene");

        Assert.IsNotNull(Object.FindObjectOfType<MainSceneControl>());
    }
}
