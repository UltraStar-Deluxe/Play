using System;
using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static AwaitableUtils;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneChangeAsyncTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [Inject]
    private UIDocument uiDocument;

    [UnityTest]
    public IEnumerator ShouldChangeScene() => ShouldChangeSceneAsync();
    private async Awaitable ShouldChangeSceneAsync() {
        LogAssert.ignoreFailingMessages = true;
        await WaitForSceneAsync(EScene.MainScene);
        await ClickButtonAsync(R.UxmlNames.aboutButton);
        await WaitForSceneAsync(EScene.AboutScene);
    }

    private async Awaitable WaitForSceneAsync(EScene scene)
    {
        await WaitForCondition($"wait for scene {scene}", TimeSpan.FromMilliseconds(1000),
            () => SceneNavigator.Instance.CurrentScene == scene);
    }

    private async Awaitable ClickButtonAsync(string uxmlName)
    {
        await WaitForCondition($"wait until button can be clicked: {uxmlName}", TimeSpan.FromSeconds(10),
            () => VisualElementUtils.IsFocusableNow(uiDocument.rootVisualElement.Q<Button>(uxmlName), uiDocument));
        uiDocument.rootVisualElement.Q<Button>(uxmlName).SendClickEvent();
    }
}
