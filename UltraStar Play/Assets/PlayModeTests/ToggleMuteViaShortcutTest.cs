using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static ConditionUtils;
using static UnityEngine.Awaitable;

public class ToggleMuteViaShortcutTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.MainScene.ToString();

    [UnityTest]
    public IEnumerator ToggleMuteShouldAffectVolume() => ToggleMuteShouldAffectVolumeAsync();
    private async Awaitable ToggleMuteShouldAffectVolumeAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given
        await ExpectNotMutedAndNonZeroVolume();

        // When
        await PressAndReleaseF10Key();

        // Then
        await ExpectMutedAndZeroVolume();
    }

    private async Awaitable ExpectNotMutedAndNonZeroVolume()
    {
        await WaitForCondition(() => !VolumeControl.Instance.IsMuted,
                new WaitForConditionConfig { description = "volume is not muted" });
        await WaitForCondition(() => AudioListener.volume > 0,
                new WaitForConditionConfig { description = "volume is non-zero" });
    }

    private async Awaitable PressAndReleaseF10Key()
    {
        InputFixture.PressAndRelease(Keyboard.current.f10Key);
        await WaitForSecondsAsync(0.1f);
    }

    private async Awaitable ExpectMutedAndZeroVolume()
    {
        await WaitForCondition(() => VolumeControl.Instance.IsMuted,
            new WaitForConditionConfig { description = "volume is muted" });
        await WaitForCondition(() => AudioListener.volume <= 0,
            new WaitForConditionConfig { description = "volume is zero" });
    }
}
