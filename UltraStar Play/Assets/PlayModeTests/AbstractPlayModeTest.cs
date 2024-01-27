using System.Collections;
using NUnit.Framework;
using Responsible;
using Responsible.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class AbstractPlayModeTest
{
    protected virtual string TestSceneName => "CommonTestScene";

    protected TestInstructionExecutor Executor { get; set; }
    protected InputTestFixture VirtualInput { get; set; }

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        this.Executor = new UnityTestInstructionExecutor();
        VirtualInput = new InputTestFixture();

        yield return LoadTestScene();
    }

    private IEnumerator LoadTestScene()
    {
        if (TestSceneName.IsNullOrEmpty())
        {
            yield break;
        }

        Debug.Log($"Loading scene {TestSceneName}");
        SceneManager.LoadScene(TestSceneName, LoadSceneMode.Single);
        yield return new WaitForEndOfFrame();
    }
}
