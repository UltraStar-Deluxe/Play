using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneChangeTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [Inject]
    private UIDocument uiDocument;

    [UnityTest]
    public IEnumerator ShouldChangeScene()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return WaitUntilScene(EScene.MainScene);
        yield return ClickButton(R.UxmlNames.aboutButton);
        yield return WaitUntilScene(EScene.AboutScene);
    }

    private CustomYieldInstruction WaitUntilScene(EScene scene)
    {
        return new WaitUntilWithTimeout($"wait for scene {scene}", TimeSpan.FromMilliseconds(1000),
            () => SceneNavigator.Instance.CurrentScene == scene);
    }

    private IEnumerator ClickButton(string uxmlName)
    {
        yield return new WaitUntilWithTimeout($"wait until button can be clicked: {uxmlName}", TimeSpan.FromSeconds(10),
            () => VisualElementUtils.IsFocusableNow(uiDocument.rootVisualElement.Q<Button>(uxmlName), uiDocument));
        uiDocument.rootVisualElement.Q<Button>(uxmlName).SendClickEvent();
    }
}
