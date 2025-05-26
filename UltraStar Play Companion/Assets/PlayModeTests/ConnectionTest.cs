using System.Collections;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class ConnectionTest : AbstractCompanionAppPlayModeTest
{
    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [UnityTest]
    [Ignore("Main game not present on CI pipeline.")]
    public IEnumerator ShouldConnect() => ShouldConnectAsync();
    private async Awaitable ShouldConnectAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ConditionUtils.WaitForConditionAsync(
            () => clientSideCompanionClientManager.IsConnected,
            new WaitForConditionConfig { description = "is connected with main game"});
    }
}
