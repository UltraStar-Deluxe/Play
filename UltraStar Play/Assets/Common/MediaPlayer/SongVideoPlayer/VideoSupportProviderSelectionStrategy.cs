using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class VideoSupportProviderSelectionStrategy
{
    public static IVideoSupportProvider Select(
        IVideoSupportProvider[] availableProviders,
        string videoUri,
        bool videoEqualsAudio,
        SongAudioPlayer songAudioPlayer,
        Settings settings)
    {
        if (availableProviders.IsNullOrEmpty())
        {
            return null;
        }
        IVideoSupportProvider provider;

        // WebView URLs
        // if (TryGetWebViewProvider(availableProviders, videoUri, out provider))
        // {
        //     return provider;
        // }
        
        // Select the video support provider of the API that has highest priority
        List<EMediaApi> mediaApis = MediaApiPriorityUtils.GetMediaApisOrderedByPriority(settings);
        foreach (EMediaApi mediaApi in mediaApis)
        {
            if (mediaApi == EMediaApi.Unity
                && TryGetUnityProvider(availableProviders, videoUri, out provider))
            {
                return provider;
            }

            // if (mediaApi == EMediaApi.Avpro
            //     && TryGetAvproProvider(availableProviders, videoUri, videoEqualsAudio, songAudioPlayer, out provider))
            // {
            //     return provider;
            // }
            //
            // if (mediaApi == EMediaApi.Vlc
            //     && TryGetVlcProvider(availableProviders, videoUri, videoEqualsAudio, songAudioPlayer, out provider))
            // {
            //     return provider;
            // }
        }

        return null;
    }

    // private static bool TryGetWebViewProvider(IVideoSupportProvider[] availableProviders, string videoUri,
    //     out IVideoSupportProvider provider)
    // {
    //     if (WebViewUtils.CanHandleWebViewUrl(videoUri)
    //         && TryGetProvider<WebViewVideoSupportProvider>(availableProviders, out provider))
    //     {
    //         return true;
    //     }
    //
    //     provider = null;
    //     return false;
    // }

    private static bool TryGetUnityProvider(IVideoSupportProvider[] availableProviders,
        string videoUri,
        out IVideoSupportProvider provider)
    {
        string extension = Path.GetExtension(videoUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(videoUri);
        
        if ((ApplicationUtils.IsUnitySupportedVideoFormat(extension) || isHttp)
            && TryGetProvider<VideoPlayerVideoSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    // private static bool TryGetAvproProvider(
    //     IVideoSupportProvider[] availableProviders,
    //     string videoUri,
    //     bool videoEqualsAudio,
    //     SongAudioPlayer songAudioPlayer,
    //     out IVideoSupportProvider provider)
    // {
    //     if (videoEqualsAudio
    //         && songAudioPlayer.CurrentAudioSupportProvider is AvproAudioSupportProvider
    //         && TryGetProvider<SongAudioPlayerAvproVideoSupportProvider>(availableProviders, out provider))
    //     {
    //         return true;
    //     }
    //     
    //     string extension = Path.GetExtension(videoUri);
    //     if (ApplicationUtils.IsAvproSupportedVideoFormat(extension)
    //         && TryGetProvider<AvproVideoSupportProvider>(availableProviders, out provider))
    //     {
    //         return true;
    //     }
    //
    //     provider = null;
    //     return false;
    // }
    //
    // private static bool TryGetVlcProvider(
    //     IVideoSupportProvider[] availableProviders,
    //     string videoUri,
    //     bool videoEqualsAudio,
    //     SongAudioPlayer songAudioPlayer,
    //     out IVideoSupportProvider provider)
    // {
    //     if (videoEqualsAudio
    //         && songAudioPlayer.CurrentAudioSupportProvider is VlcAudioSupportProvider
    //         && TryGetProvider<SongAudioPlayerVlcVideoSupportProvider>(availableProviders, out provider))
    //     {
    //         return true;
    //     }
    //     
    //     string extension = Path.GetExtension(videoUri);
    //     if (ApplicationUtils.IsVlcSupportedVideoFormat(extension)
    //         && TryGetProvider<VlcVideoSupportProvider>(availableProviders, out provider))
    //     {
    //         return true;
    //     }
    //
    //     provider = null;
    //     return false;
    // }

    private static bool TryGetProvider<T>(IVideoSupportProvider[] availableProviders, out IVideoSupportProvider provider) where T : IVideoSupportProvider
    {
        provider = availableProviders.OfType<T>().FirstOrDefault();
        return provider != null;
    }
}
