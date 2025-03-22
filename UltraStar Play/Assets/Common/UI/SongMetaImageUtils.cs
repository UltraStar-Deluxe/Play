using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class SongMetaImageUtils
{
    private static YouTubeCoverImageProvider youTubeCoverImageProvider = new();

    public static async Awaitable<string> GetBackgroundOrCoverImageUriAsync(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return uri;
        }

        // Try the cover image as fallback
        uri = SongMetaUtils.GetCoverUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return uri;
        }

        // Try to find an image via mods
        List<ISongBackgroundImageProvider> songBackgroundImageProviders = ModManager.GetModObjects<ISongBackgroundImageProvider>();
        if (songBackgroundImageProviders.IsNullOrEmpty())
        {
            return "";
        }

        foreach (ISongBackgroundImageProvider songBackgroundImageProvider in songBackgroundImageProviders)
        {
            string backgroundImageUri = await songBackgroundImageProvider.GetBackgroundImageUriAsync(songMeta);
            if (!backgroundImageUri.IsNullOrEmpty())
            {
                return backgroundImageUri;
            }
        }
        return "";
    }

    public static async Awaitable<string> GetCoverOrBackgroundImageUriAsync(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetCoverUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return uri;
        }

        // Try the background image as fallback
        uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return uri;
        }

        // Try to find an image via mods
        List<ISongCoverImageProvider> songCoverImageProviders = ModManager.GetModObjects<ISongCoverImageProvider>()
            .Union(new List<ISongCoverImageProvider>() { youTubeCoverImageProvider })
            .ToList();
        if (songCoverImageProviders.IsNullOrEmpty())
        {
            return "";
        }

        foreach (ISongCoverImageProvider songCoverImageProvider in songCoverImageProviders)
        {
            string coverImageUri = await songCoverImageProvider.GetCoverImageUriAsync(songMeta);
            if (!coverImageUri.IsNullOrEmpty())
            {
                return coverImageUri;
            }
        }

        return "";
    }

    public static async Awaitable SetCoverOrBackgroundImageAsync(CancellationToken cancellationToken, SongMeta songMeta, params VisualElement[] visualElements)
    {
        try
        {
            string uri = await GetCoverOrBackgroundImageUriAsync(songMeta);
            await SetCoverOrBackgroundImageFromUriAsync(cancellationToken, songMeta, uri, visualElements);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetDefaultSongImageAndColor(songMeta, visualElements);
        }
    }

    public static void SetCoverOrBackgroundImageAsync(Sprite sprite, params VisualElement[] visualElements)
    {
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.backgroundImage = new StyleBackground(sprite);
            visualElement.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
        }
    }

    public static async Awaitable SetCoverOrBackgroundImageFromUriAsync(CancellationToken cancellationToken, SongMeta songMeta, string uri, params VisualElement[] visualElements)
    {
        if (uri.IsNullOrEmpty())
        {
            SetDefaultSongImageAndColor(songMeta, visualElements);
            return;
        }

        try
        {
            Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(uri);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            SetCoverOrBackgroundImageAsync(loadedSprite, visualElements);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetDefaultSongImageAndColor(songMeta, visualElements);
        }
    }

    public static void SetDefaultSongImageAndColor(SongMeta songMeta, params VisualElement[] visualElements)
    {
        SetDefaultSongImage(visualElements);
        SetDefaultSongImageColor(songMeta, visualElements);
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

        Color32 color = ColorGenerationUtils.FromString(songMeta.GetArtistDashTitle());
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.unityBackgroundImageTintColor = new StyleColor(color);
        }
    }
}
