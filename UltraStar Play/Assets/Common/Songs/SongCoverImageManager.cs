using System;
using System.Threading;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongCoverImageManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SongCoverImageManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongCoverImageManager>();

    [Inject]
    private SongMediaUriResolverManager songMediaUriResolverManager;
    
    public async Awaitable SetCoverOrBackgroundImageAsync(CancellationToken cancellationToken, SongMeta songMeta, params VisualElement[] visualElements)
    {
        try
        {
            string uri = await songMediaUriResolverManager.ResolveCoverOrBackgroundUriAsync(songMeta);
            await SetCoverOrBackgroundImageFromUriAsync(cancellationToken, songMeta, uri, visualElements);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetDefaultCoverImageAndColor(songMeta, visualElements);
        }
    }
    
    public static async Awaitable SetCoverOrBackgroundImageFromUriAsync(CancellationToken cancellationToken, SongMeta songMeta, string uri, params VisualElement[] visualElements)
    {
        if (uri.IsNullOrEmpty())
        {
            SetDefaultCoverImageAndColor(songMeta, visualElements);
            return;
        }

        try
        {
            Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(uri);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            SetCoverOrBackgroundImageFromSpriteAsync(loadedSprite, visualElements);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetDefaultCoverImageAndColor(songMeta, visualElements);
        }
    }
    
    public static void SetCoverOrBackgroundImageFromSpriteAsync(Sprite sprite, params VisualElement[] visualElements)
    {
        foreach (VisualElement visualElement in visualElements)
        {
            visualElement.style.backgroundImage = new StyleBackground(sprite);
            visualElement.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
        }
    }

    public static void SetDefaultCoverImageAndColor(SongMeta songMeta, params VisualElement[] visualElements)
    {
        SetDefaultCoverImage(visualElements);
        SetDefaultCoverColor(songMeta, visualElements);
    }

    public static void SetDefaultCoverImage(params VisualElement[] visualElements)
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

    public static void SetDefaultCoverColor(SongMeta songMeta, params VisualElement[] visualElements)
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
    
    protected override object GetInstance()
    {
        return Instance;
    }
}
