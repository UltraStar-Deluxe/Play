using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongDetailsPageObject : INeedInjection
{
    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;

    [Inject]
    private SongListPageObject songListPageObject;

    [Inject(UxmlName = R.UxmlNames.enqueueButton)]
    private Button enqueueButton;

    [Inject(UxmlName = R.UxmlNames.favoriteButton)]
    private Button favoriteButton;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private VisualElement favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private VisualElement noFavoriteIcon;

    [Inject(UxmlName = R.UxmlNames.songImage)]
    private VisualElement songImage;

    [Inject(UxmlName = R.UxmlNames.songArtistLabel)]
    private Label songArtistLabel;

    [Inject(UxmlName = R.UxmlNames.songTitleLabel)]
    private Label songTitleLabel;

    public async Awaitable OpenAsync(string songTitle)
    {
        await songListPageObject.OpenAsync();
        songListPageObject.SetSearchText(songTitle);
        songListPageObject.GetFirstSongEntryButton().SendClickEvent();

        await Awaitable.WaitForSecondsAsync(1);
    }

    public void Enqueue()
    {
        enqueueButton.SendClickEvent();
    }

    public string GetArtist()
    {
        return songArtistLabel.text;
    }

    public string GetTitle()
    {
        return songTitleLabel.text;
    }

    public Texture2D GetImage()
    {
        return songImage.resolvedStyle.backgroundImage.texture;
    }

    public void ToggleFavorite()
    {
        favoriteButton.SendClickEvent();
    }

    public bool IsFavoriteIconShown()
    {
        return favoriteIcon.IsVisibleByDisplay()
            && !noFavoriteIcon.IsVisibleByDisplay();
    }
}
