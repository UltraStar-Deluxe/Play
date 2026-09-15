using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

public class SongMediaUriResolverManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SongMediaUriResolverManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongMediaUriResolverManager>();

    private static readonly YouTubeCoverUriProvider youTubeCoverUriProvider = new();
    private static readonly UsdbVideoTagSyntaxMediaResolver usdbVideoTagSyntaxMediaResolver = new();

    [Inject]
    private ModManager modManager;

    public string ResolveAudioUri(SongMeta songMeta)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (SongMetaUtils.ResourceExists(songMeta, audioUri))
        {
            return audioUri;
        }
        
        // Try to find an audio uri via mods
        return ModManager.GetModObjects<IAudioUriProvider>()
                   .Union(new List<IAudioUriProvider>() { usdbVideoTagSyntaxMediaResolver })
                   .Select(provider => SafeGetUri(() => provider.GetAudioUri(songMeta)))
                   .FirstOrDefault(uri => !uri.IsNullOrEmpty())
               ?? audioUri;
    }
    
    public string ResolveVideoUri(SongMeta songMeta)
    {
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
        if (SongMetaUtils.ResourceExists(songMeta, videoUri))
        {
            return videoUri;
        }
        
        return ModManager.GetModObjects<IVideoUriProvider>()
                   .Union(new List<IVideoUriProvider>() { usdbVideoTagSyntaxMediaResolver })
                   .Select(provider => SafeGetUri(() => provider.GetVideoUri(songMeta)))
                   .FirstOrDefault(uri => !uri.IsNullOrEmpty())
               ?? videoUri;
    }
    
    public async Awaitable<string> ResolveCoverOrBackgroundUriAsync(SongMeta songMeta)
    {
        return await GetCoverOrBackgroundImageUriAsync(songMeta);
    }
    
    public async Awaitable<string> ResolveBackgroundOrCoverUriAsync(SongMeta songMeta)
    {
        return await GetBackgroundOrCoverImageUriAsync(songMeta);
    }
    
    protected override object GetInstance()
    {
        return Instance;
    }
    
    private static async Awaitable<string> GetBackgroundOrCoverImageUriAsync(SongMeta songMeta)
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
        List<IBackgroundUriProvider> providers = ModManager.GetModObjects<IBackgroundUriProvider>()
            // youTubeCoverUriProvider is not added here because the YouTube video is used as background
            .Union(new List<IBackgroundUriProvider>() { usdbVideoTagSyntaxMediaResolver })
            .ToList();
        if (providers.IsNullOrEmpty())
        {
            return "";
        }

        foreach (IBackgroundUriProvider songBackgroundImageProvider in providers)
        {
            string backgroundImageUri = await songBackgroundImageProvider.GetBackgroundUriAsync(songMeta);
            if (!backgroundImageUri.IsNullOrEmpty())
            {
                return backgroundImageUri;
            }
        }
        return "";
    }

    private static async Awaitable<string> GetCoverOrBackgroundImageUriAsync(SongMeta songMeta)
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
        List<ICoverUriProvider> providers = ModManager.GetModObjects<ICoverUriProvider>()
            .Union(new List<ICoverUriProvider>() { usdbVideoTagSyntaxMediaResolver, youTubeCoverUriProvider })
            .ToList();
        if (providers.IsNullOrEmpty())
        {
            return "";
        }

        foreach (ICoverUriProvider songCoverImageProvider in providers)
        {
            string coverImageUri = await songCoverImageProvider.GetCoverUriAsync(songMeta);
            if (!coverImageUri.IsNullOrEmpty())
            {
                return coverImageUri;
            }
        }

        return "";
    }

    private string SafeGetUri(Func<string> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }
}
