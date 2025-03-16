using System.Collections;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class SongDetailsTest : AbstractConnectedCompanionAppPlayModeTest
{
    private readonly string songTitle = "Kryptonite";
    private readonly string songArtist = "3 Doors Down";

    [Inject(UxmlName = R.UxmlNames.songImage)]
    private VisualElement songImage;

    [Inject(UxmlName = R.UxmlNames.songArtistLabel)]
    private Label songArtistLabel;

    [Inject(UxmlName = R.UxmlNames.songTitleLabel)]
    private Label songTitleLabel;

    [Inject(UxmlName = R.UxmlNames.favoriteButton)]
    private Button favoriteButton;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private VisualElement noFavoriteIcon;

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
        await ConditionUtils.WaitForConditionAsync(() => songTitleLabel.text == songTitle,
            new WaitForConditionConfig { description = "shows song title" });

        await ConditionUtils.WaitForConditionAsync(() => songArtistLabel.text == songArtist,
            new WaitForConditionConfig { description = "shows song artist" });

        await ConditionUtils.WaitForConditionAsync(() => songImage.resolvedStyle.backgroundImage.sprite != null,
            new WaitForConditionConfig { description = "shows cover image" });
    }

    [UnityTest]
    public IEnumerator ToggleFavoriteShouldChangeIcon() => ToggleFavoriteShouldChangeIconAsync();
    private async Awaitable ToggleFavoriteShouldChangeIconAsync()
    {
        // Given
        bool favoriteIconVisibleByDisplay = favoriteIcon.IsVisibleByDisplay();
        bool noFavoriteIconVisibleByDisplay = noFavoriteIcon.IsVisibleByDisplay();

        // When
        await songDetailsPageObject.OpenAsync(songTitle);
        favoriteButton.SendClickEvent();

        // Then
        await ConditionUtils.WaitForConditionAsync(() =>
                favoriteIcon.IsVisibleByDisplay() == !favoriteIconVisibleByDisplay
                && noFavoriteIcon.IsVisibleByDisplay() == !noFavoriteIconVisibleByDisplay,
            new WaitForConditionConfig { description = "has toggled favorite icon" });
    }
}
