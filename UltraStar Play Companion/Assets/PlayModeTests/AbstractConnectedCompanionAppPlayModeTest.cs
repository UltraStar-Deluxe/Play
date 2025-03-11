using UnityEngine;

public abstract class AbstractConnectedCompanionAppPlayModeTest : AbstractCompanionAppPlayModeTest
{
    protected virtual string TestSceneName => "MainScene";

    protected override async Awaitable SetUpTestFixtureAsync()
    {
        await base.SetUpTestFixtureAsync();

        await ConditionUtils.WaitForConditionAsync(
            () => ClientSideCompanionClientManager.Instance.IsConnected,
            new WaitForConditionConfig { description = "is connected with main game"});
    }
}
