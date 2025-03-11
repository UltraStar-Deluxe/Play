using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class ConnectionTest : AbstractCompanionAppPlayModeTest
{
    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [UnityTest]
    public IEnumerator ShouldConnect() => ShouldConnectAsync();
    private async Awaitable ShouldConnectAsync()
    {
        LogAssert.ignoreFailingMessages = true;
        await ConditionUtils.WaitForConditionAsync(
            () => clientSideCompanionClientManager.IsConnected,
            new WaitForConditionConfig { description = "is connected with main game"});
    }
}
