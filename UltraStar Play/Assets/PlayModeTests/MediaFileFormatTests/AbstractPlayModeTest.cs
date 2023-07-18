using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class AbstractPlayModeTest
{
    protected virtual string TestSceneName => "CommonTestScene";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Debug.Log("OneTimeSetUp start");
        LoadTestScene();
        Debug.Log("OneTimeSetUp done");
    }

    private void LoadTestScene()
    {
        Debug.Log($"Loading scene {TestSceneName}");
        SceneManager.LoadScene(TestSceneName, LoadSceneMode.Single);
    }
}
