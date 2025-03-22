using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class SongDetailsTest : AbstractConnectedCompanionAppPlayModeTest
{
    private readonly string songTitle = "Kryptonite";
    private readonly string songArtist = "3 Doors Down";

    [Inject]
    private SongDetailsPageObject songDetailsPageObject;

    [UnityTest]
    public IEnumerator ShouldOpenSongDetails() => ShouldOpenSongDetailsAsync();
    private async Awaitable ShouldOpenSongDetailsAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // When
        await songDetailsPageObject.OpenAsync(songTitle);

        // Then
        await ConditionUtils.WaitForConditionAsync(() => songDetailsPageObject.GetArtist() == songArtist,
            new WaitForConditionConfig { description = "shows song artist" });

        await ConditionUtils.WaitForConditionAsync(() => songDetailsPageObject.GetTitle() == songTitle,
            new WaitForConditionConfig { description = "shows song title" });

        await ConditionUtils.WaitForConditionAsync(() => songDetailsPageObject.GetImage() != null,
            new WaitForConditionConfig { description = "shows cover image" });
    }

    [UnityTest]
    public IEnumerator ToggleFavoriteShouldChangeIcon() => ToggleFavoriteShouldChangeIconAsync();
    private async Awaitable ToggleFavoriteShouldChangeIconAsync()
    {
        // Given
        await songDetailsPageObject.OpenAsync(songTitle);
        bool isFavoriteIconShown = songDetailsPageObject.IsFavoriteIconShown();

        // When
        songDetailsPageObject.ToggleFavorite();

        // Then
        await ConditionUtils.WaitForConditionAsync(() =>
                songDetailsPageObject.IsFavoriteIconShown() == !isFavoriteIconShown,
            new WaitForConditionConfig { description = "has toggled favorite icon" });
    }
}
