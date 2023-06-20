using UnityEngine;
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
        if (uri.IsNullOrEmpty())
        {
            SetDefaultSongImage(visualElements);
            SetDefaultSongImageColor(songMeta, visualElements);
            return;
        }
        
        ImageManager.LoadSpriteFromUri(uri,
            loadedSprite =>
            {
                foreach (VisualElement visualElement in visualElements)
                {
                    visualElement.style.backgroundImage = new StyleBackground(loadedSprite);
                    visualElement.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
                }
            },
            _ =>
            {
                SetDefaultSongImage(visualElements);
                SetDefaultSongImageColor(songMeta, visualElements);
            });
    }

    public static void SetDefaultSongImage(params VisualElement[] visualElements)
    {
        if (visualElements.IsNullOrEmpty())
        {
            return;
        }
        
        Sprite defaultCoverImage = UiManager.Instance.defaultSongImage;
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.backgroundImage = new StyleBackground(defaultCoverImage);
        }
    }
    
    public static void SetDefaultSongImageColor(SongMeta songMeta, params VisualElement[] visualElements)
    {
        if (songMeta == null
            || visualElements.IsNullOrEmpty())
        {
            return;
        }
        
        Color32 color = SongMetaUtils.CreateColorForSongMeta(songMeta);
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.unityBackgroundImageTintColor = new StyleColor(color);
        }
    }
}
