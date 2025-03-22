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
        await ExpectNotMutedAndNonZeroVolumeAsync();

        // When
        await PressAndReleaseF10KeyAsync();

        // Then
        await ExpectMutedAndZeroVolumeAsync();
    }

    private async Awaitable ExpectNotMutedAndNonZeroVolumeAsync()
    {
        await WaitForConditionAsync(() => !VolumeManager.Instance.IsMuted,
                new WaitForConditionConfig { description = "volume is not muted" });
        await WaitForConditionAsync(() => AudioListener.volume > 0,
                new WaitForConditionConfig { description = "volume is non-zero" });
    }

    private async Awaitable PressAndReleaseF10KeyAsync()
    {
        InputFixture.PressAndRelease(Keyboard.current.f10Key);
        await WaitForSecondsAsync(0.1f);
    }

    private async Awaitable ExpectMutedAndZeroVolumeAsync()
    {
        await WaitForConditionAsync(() => VolumeManager.Instance.IsMuted,
            new WaitForConditionConfig { description = "volume is muted" });
        await WaitForConditionAsync(() => AudioListener.volume <= 0,
            new WaitForConditionConfig { description = "volume is zero" });
    }
}
