using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class SongMetaImageUtils
{
    public static IObservable<string> GetBackgroundOrCoverImageUri(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return Observable.Return(uri);
        }

        // Try the cover image as fallback
        uri = SongMetaUtils.GetCoverUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return Observable.Return(uri);
        }

        // Try to find an image via mods
        List<ISongBackgroundImageProvider> songBackgroundImageProviders = ModManager.GetModObjects<ISongBackgroundImageProvider>();
        if (songBackgroundImageProviders.IsNullOrEmpty())
        {
            return Observable.Return("");
        }
        return songBackgroundImageProviders
            .Select(songBackgroundImageProvider => songBackgroundImageProvider.GetBackgroundImageUri(songMeta))
            .Merge()
            .FirstOrDefault()
            .ObserveOnMainThread();
    }

    public static IObservable<string> GetCoverOrBackgroundImageUri(SongMeta songMeta)
    {
        string uri = SongMetaUtils.GetCoverUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return Observable.Return(uri);
        }

        // Try the background image as fallback
        uri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, uri))
        {
            return Observable.Return(uri);
        }

        // Try to find an image via mods
        List<ISongCoverImageProvider> songCoverImageProviders = ModManager.GetModObjects<ISongCoverImageProvider>();
        if (songCoverImageProviders.IsNullOrEmpty())
        {
            return Observable.Return("");
        }
        return songCoverImageProviders
            .Select(songCoverImageProvider => songCoverImageProvider.GetCoverImageUri(songMeta))
            .Merge()
            .FirstOrDefault()
            .ObserveOnMainThread();
    }

    public static void SetCoverOrBackgroundImage(SongMeta songMeta, params VisualElement[] visualElements)
    {
        GetCoverOrBackgroundImageUri(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                SetDefaultSongImageAndColor(songMeta, visualElements);
            })
            .Subscribe(uri => SetCoverOrBackgroundImageFromUri(songMeta, uri, visualElements));
    }

    public static void SetCoverOrBackgroundImageFromUri(SongMeta songMeta, string uri, params VisualElement[] visualElements)
    {
        if (uri.IsNullOrEmpty())
        {
            SetDefaultSongImageAndColor(songMeta, visualElements);
            return;
        }

        ImageManager.LoadSpriteFromUri(uri)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                SetDefaultSongImageAndColor(songMeta, visualElements);
            })
            .Subscribe(loadedSprite =>
            {
                foreach (VisualElement visualElement in visualElements)
                {
                    visualElement.style.backgroundImage = new StyleBackground(loadedSprite);
                    visualElement.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
                }
            });
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

        Color32 color = SongMetaUtils.CreateColorForSongMeta(songMeta);
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.unityBackgroundImageTintColor = new StyleColor(color);
        }
    }
}
