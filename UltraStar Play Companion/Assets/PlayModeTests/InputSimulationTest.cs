using System.Collections;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class InputSimulationTest : AbstractConnectedCompanionAppPlayModeTest
{
    [Inject]
    private InputSimulationControl inputSimulationControl;

    [UnityTest]
    [Ignore("Main game not present on CI pipeline.")]
    public IEnumerator ShouldSendRequestSuccessfully() => ShouldSendRequestSuccessfullyAsync();
    private async Awaitable ShouldSendRequestSuccessfullyAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Sending request should not throw exception
        await inputSimulationControl.SendSimulateLeftMouseButtonClickRequestAsync();
    }
}
