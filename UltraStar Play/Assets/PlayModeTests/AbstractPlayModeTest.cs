using System.Collections;
using Responsible.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class AbstractPlayModeTest : AbstractResponsibleTest
{
    protected virtual string TestSceneName => "CommonTestScene";

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        SettingsManager.SettingsLoaderSaver = new TestSettingsLoaderSaver();

        yield return LoadTestScene();

        AssertUtils.HasType<TestSettings>(SettingsManager.Instance.Settings);
        ConfigureTestSettings(SettingsManager.Instance.Settings as TestSettings);

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();

        Executor = new UnityTestInstructionExecutor();

        yield return new WaitForEndOfFrame();
    }

    protected virtual void ConfigureTestSettings(TestSettings settings)
    {
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
