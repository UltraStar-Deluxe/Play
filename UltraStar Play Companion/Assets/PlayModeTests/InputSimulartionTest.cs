using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class InputSimulationTest : AbstractConnectedCompanionAppPlayModeTest
{
    [Inject]
    private InputSimulationControl inputSimulationControl;

    [UnityTest]
    public IEnumerator ShouldSendRequestSuccessfully() => ShouldSendRequestSuccessfullyAsync();
    private async Awaitable ShouldSendRequestSuccessfullyAsync()
    {
        LogAssert.ignoreFailingMessages = true;

        // Sending request should not throw exception
        await inputSimulationControl.SendSimulateLeftMouseButtonClickRequestAsync();
    }
}
