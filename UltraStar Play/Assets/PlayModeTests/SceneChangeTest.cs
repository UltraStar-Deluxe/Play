using System;
using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static VisualElementTestUtils;
using static SceneConditionTestUtils;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneChangeTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [Inject]
    private UIDocument uiDocument;

    [UnityTest]
    public IEnumerator ShouldChangeScene() => ShouldChangeSceneAsync();
    private async Awaitable ShouldChangeSceneAsync() {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectSceneAsync(EScene.MainScene);
        await ClickButtonAsync(R.UxmlNames.aboutButton);
        await ExpectSceneAsync(EScene.AboutScene);
    }
}
