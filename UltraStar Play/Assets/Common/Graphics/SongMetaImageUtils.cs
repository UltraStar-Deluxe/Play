using UnityEngine.UIElements;

public static class SongMetaImageUtils
{
    public static string GetBackgroundOrCoverImageUri(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (!uri.IsNullOrEmpty())
        {
            return uri;

        }

        // Try the cover image as fallback
        uri = SongMetaUtils.GetCoverUri(songMeta);
        if (!uri.IsNullOrEmpty())
        {
            return uri;
        }

        return null;
    }
    
    public static string GetCoverOrBackgroundImageUri(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetCoverUri(songMeta);
        if (!uri.IsNullOrEmpty())
        {
            return uri;
        }

        // Try the background image as fallback
        uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (!uri.IsNullOrEmpty())
        {
            return uri;
        }

        return null;
    }
    
    public static void SetCoverOrBackgroundImage(SongMeta songMeta, params VisualElement[] visualElements)
    {
        string uri = GetCoverOrBackgroundImageUri(songMeta);
        ImageManager.LoadSpriteFromUri(uri, loadedSprite =>
        {
            foreach (VisualElement visualElement in visualElements)
            {
                visualElement.style.backgroundImage = new StyleBackground(loadedSprite);
            }
        });
    }
}
