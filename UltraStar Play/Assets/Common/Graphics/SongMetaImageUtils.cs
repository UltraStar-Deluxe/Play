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
            return Observable.Empty<string>();
        }
        return songBackgroundImageProviders
            .Select(it => it.GetBackgroundImageUri(songMeta).FirstOrDefault())
            .FirstOrDefault();

        // if (SongMetaMissingImageProviderManager.TryFindBackgroundImageInFolder(songMeta.Directory, out uri)
        //     && SongMetaUtils.ResourceExists(songMeta, uri))
        // {
        //     return Observable.Return(uri);
        // }
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
            return Observable.Empty<string>();
        }
        return songCoverImageProviders
            .Select(it => it.GetCoverImageUri(songMeta).FirstOrDefault())
            .FirstOrDefault();

        // // Try to find an image in the song's folder
        // if (SongMetaMissingImageProviderManager.TryFindCoverImageInFolder(songMeta.Directory, out uri)
        //     && SongMetaUtils.ResourceExists(songMeta, uri))
        // {
        //     return uri;
        // }
    }

    public static void SetCoverOrBackgroundImage(SongMeta songMeta, params VisualElement[] visualElements)
    {
        GetCoverOrBackgroundImageUri(songMeta)
            .ObserveOnMainThread()
            .Subscribe(uri => SetCoverOrBackgroundImageFromUri(songMeta, uri, visualElements));
    }

    private static void SetCoverOrBackgroundImageFromUri(SongMeta songMeta, string uri, params VisualElement[] visualElements)
    {
        if (uri.IsNullOrEmpty())
        {
            SetDefaultSongImage(visualElements);
            SetDefaultSongImageColor(songMeta, visualElements);
            return;
        }

        ImageManager.LoadSpriteFromUri(uri)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                SetDefaultSongImage(visualElements);
                SetDefaultSongImageColor(songMeta, visualElements);
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
