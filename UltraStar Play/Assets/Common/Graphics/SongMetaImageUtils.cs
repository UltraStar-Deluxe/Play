using UnityEngine.UIElements;

public static class SongMetaImageUtils
{
    public static void SetCoverOrBackgroundImage(SongMeta coverSongMeta, params VisualElement[] visualElements)
    {
        string coverUri = SongMetaUtils.GetCoverUri(coverSongMeta);
        if (coverUri.IsNullOrEmpty())
        {
            // Try the background image as fallback
            coverUri = SongMetaUtils.GetBackgroundUri(coverSongMeta);
            if (coverUri.IsNullOrEmpty())
            {
                return;
            }
        }

        ImageManager.LoadSpriteFromUri(coverUri, loadedSprite =>
        {
            foreach (VisualElement visualElement in visualElements)
            {
                visualElement.style.backgroundImage = new StyleBackground(loadedSprite);
            }
        });
    }
}
