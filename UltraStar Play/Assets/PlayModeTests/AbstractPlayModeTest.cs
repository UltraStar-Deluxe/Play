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
        StatisticsManager.StatisticsLoaderSaver = new TestStatisticsLoaderSaver();

        yield return LoadTestScene();

        AssertUtils.HasType<TestSettings>(SettingsManager.Instance.Settings);
        ConfigureTestSettings(SettingsManager.Instance.Settings as TestSettings);
        AssertUtils.HasType<TestStatistics>(StatisticsManager.Instance.Statistics);
        ConfigureTestStatistics(StatisticsManager.Instance.Statistics as TestStatistics);

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();

        Executor = new UnityTestInstructionExecutor();

        yield return new WaitForEndOfFrame();
    }

    protected virtual void ConfigureTestStatistics(TestStatistics statistics)
    {
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

        Debug.Log($"Loading test scene {TestSceneName}");
        SceneManager.LoadScene(TestSceneName, LoadSceneMode.Single);
        yield return new WaitForEndOfFrame();
    }
}
