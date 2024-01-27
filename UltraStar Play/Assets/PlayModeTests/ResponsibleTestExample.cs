using System.Collections;
using Responsible;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Responsible.Bdd.Keywords;
using static Responsible.Responsibly;

public class ResponsibleTestExample : AbstractPlayModeTest
{
    [UnityTest]
    public IEnumerator ResponsibleExampleTest() => WaitForCondition(
        "wait for 4 < 11",
        () => 4 < 11)
        .ExpectWithinSeconds(1)
        .ToYieldInstruction(Executor);

    [UnityTest]
    public IEnumerator ToggleMuteViaF10() => this.Executor.YieldScenario(
        Scenario("should toggle mute when pressing F10"),
        Given("loaded main scene and is not muted", LoadMainSceneAssertNotMuted()),
        When("pressing F10", PressAndReleaseF10Key()),
        Then("is muted", AssertMuted())
    );

    private ITestInstruction<object> LoadMainSceneAssertNotMuted() => Do(
            "load main scene",
            () => SceneNavigator.Instance.LoadScene(EScene.MainScene))
        .ContinueWith(
            WaitForCondition(
                    "is not muted",
                    () => !VolumeControl.Instance.IsMuted)
                .AndThen(WaitForCondition(
                    "volume not zero",
                    () => AudioListener.volume > 0))
                .ExpectWithinSeconds(1));

    private ITestInstruction<object> PressAndReleaseF10Key() => Do(
        "press and release F10 key",
        () => VirtualInput.PressAndRelease(Keyboard.current.f10Key));

    private ITestInstruction<object> AssertMuted() => WaitForCondition(
        "is muted",
        () => VolumeControl.Instance.IsMuted

            ).AndThen(WaitForCondition(
        "volume is zero",
        () => AudioListener.volume <= 0))
        .ExpectWithinSeconds(1);

}
