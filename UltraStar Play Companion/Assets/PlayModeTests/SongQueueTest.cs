using System.Collections;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class SongQueueTest : AbstractConnectedCompanionAppPlayModeTest
{
    private readonly string songTitle = "Kryptonite";

    [Inject]
    private SongQueuePageObject songQueuePageObject;

    [Inject]
    private SongDetailsPageObject songDetailsPageObject;

    [UnityTest]
    [Ignore("Main game not present on CI pipeline.")]
    [Order(1)]
    public IEnumerator ShouldEnqueueSong() => ShouldEnqueueSongAsync();
    private async Awaitable ShouldEnqueueSongAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // When
        await songDetailsPageObject.OpenAsync(songTitle);
        songDetailsPageObject.Enqueue();
        await songQueuePageObject.OpenAsync();

        // Then
        await ConditionUtils.WaitForConditionAsync(() => songQueuePageObject.GetEntries().Count > 0,
            new WaitForConditionConfig { description = "song queue has an entry" });
    }

    [UnityTest]
    [Ignore("Main game not present on CI pipeline.")]
    [Order(2)]
    public IEnumerator ShouldRemoveSongQueueEntries() => ShouldRemoveSongQueueEntriesAsync();
    private async Awaitable ShouldRemoveSongQueueEntriesAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // When
        await songQueuePageObject.OpenAsync();
        await songQueuePageObject.RemoveAllAsync();

        // Then
        await ConditionUtils.WaitForConditionAsync(() => songQueuePageObject.GetEntries().Count == 0,
            new WaitForConditionConfig { description = "song queue is empty" });
    }
}
