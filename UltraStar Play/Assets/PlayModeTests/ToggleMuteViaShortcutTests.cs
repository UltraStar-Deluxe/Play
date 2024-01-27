using System.Collections;
using Responsible;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Responsible.Bdd.Keywords;
using static Responsible.Responsibly;

public class ToggleMuteViaShortcutTests : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [UnityTest]
    public IEnumerator ToggleMuteViaShortcutTest() => this.Executor.YieldScenario(
        Scenario("should toggle mute when pressing shortcut"),
        Given("is not muted", LoadMainSceneAssertNotMuted()),
        When("pressed F10", PressAndReleaseF10Key()),
        Then("is muted", AssertMuted())
    );

    private ITestInstruction<object> LoadMainSceneAssertNotMuted() => WaitForCondition(
            "is not muted",
            () => !VolumeControl.Instance.IsMuted)
        .AndThen(WaitForCondition(
            "volume not zero",
            () => AudioListener.volume > 0))
        .ExpectWithinSeconds(1);

    private ITestInstruction<object> PressAndReleaseF10Key() => Do(
        "press and release F10 key",
        () => InputFixture.PressAndRelease(Keyboard.current.f10Key));

    private ITestInstruction<object> AssertMuted() => WaitForCondition(
            "is muted",
            () => VolumeControl.Instance.IsMuted
        ).AndThen(WaitForCondition(
            "volume is zero",
            () => AudioListener.volume <= 0))
        .ExpectWithinSeconds(1);
}
