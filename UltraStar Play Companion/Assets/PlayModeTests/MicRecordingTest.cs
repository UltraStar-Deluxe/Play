using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class MicRecordingTest : AbstractConnectedCompanionAppPlayModeTest
{
    [Inject]
    private ClientSideMicDataSender clientSideMicDataSender;

    [Inject(UxmlName = R.UxmlNames.toggleRecordingButton)]
    private Button toggleRecordingButton;

    [UnityTest]
    [Ignore("Main game not present on CI pipeline.")]
    public IEnumerator ShouldFireBeatPitchEvents() => ShouldFireBeatPitchEventsAsync();
    private async Awaitable ShouldFireBeatPitchEventsAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given
        List<BeatPitchEventsDto> beatPitchEventsDtos = new();
        clientSideMicDataSender.BeatPitchEventsDtoEventStream
            .Subscribe(evt => beatPitchEventsDtos.Add(evt));

        // When
        toggleRecordingButton.SendClickEvent();

        // Then
        await ConditionUtils.WaitForConditionAsync(() => beatPitchEventsDtos.Count > 0,
            new WaitForConditionConfig { description = "fired beat pitch events" });
    }
}
