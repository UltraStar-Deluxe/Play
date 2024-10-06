using System.Collections;
using Responsible;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static Responsible.Responsibly;

public class ToggleMuteViaShortcutTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [UnityTest]
    public IEnumerator ToggleMuteShouldAffectVolume() => ExpectNotMutedAndNonZeroVolume()
        .ContinueWith(PressAndReleaseF10Key())
        .ContinueWith(ExpectMutedAndZeroVolume())
        .ToYieldInstruction(Executor);

    private ITestInstruction<object> ExpectNotMutedAndNonZeroVolume() => WaitForCondition(
            "is not muted",
            () => !VolumeControl.Instance.IsMuted)
        .AndThen(WaitForCondition(
            "volume not zero",
            () => AudioListener.volume > 0))
        .ExpectWithinSeconds(10);

    private ITestInstruction<object> PressAndReleaseF10Key() => Do(
        "press and release F10 key",
        () => InputFixture.PressAndRelease(Keyboard.current.f10Key));

    private ITestInstruction<object> ExpectMutedAndZeroVolume() => WaitForCondition(
            "is muted",
            () => VolumeControl.Instance.IsMuted
        ).AndThen(WaitForCondition(
            "volume is zero",
            () => AudioListener.volume <= 0))
        .ExpectWithinSeconds(10);
}
