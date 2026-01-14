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

        // WebView URLs
        if (WebViewUtils.CanHandleWebViewUrl(videoUri)
            && TryGetProvider<WebViewVideoSupportProvider>(availableProviders, out IVideoSupportProvider webViewProvider))
        {
            return webViewProvider;
        }

        string extension = Path.GetExtension(videoUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(videoUri);
        
        // Unity VideoPlayer is default, also for HTTP URLs
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && (ApplicationUtils.IsUnitySupportedVideoFormat(extension) || isHttp)
            && TryGetProvider<VideoPlayerVideoSupportProvider>(availableProviders, out IVideoSupportProvider videoPlayerProvider))
        {
            return videoPlayerProvider;
        }
        
        // When audio and video are the same file, reuse the audio provider from VLC respectively AVPro.
        if (videoEqualsAudio)
        {
            if (songAudioPlayer.CurrentAudioSupportProvider is AvproAudioSupportProvider
                && TryGetProvider<SongAudioPlayerAvproVideoSupportProvider>(availableProviders, out IVideoSupportProvider songAudioPlayerAvproProvider))
            {
                return songAudioPlayerAvproProvider;
            }
            
            if (songAudioPlayer.CurrentAudioSupportProvider is VlcAudioSupportProvider
                && TryGetProvider<SongAudioPlayerVlcVideoSupportProvider>(availableProviders, out IVideoSupportProvider songAudioPlayerVlcProvider))
            {
                return songAudioPlayerVlcProvider;
            }
        }

        // AVPro as fallback
        if (settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && ApplicationUtils.IsAvproSupportedVideoFormat(extension)
            && TryGetProvider<AvproVideoSupportProvider>(availableProviders, out IVideoSupportProvider avproProvider))
        {
            return avproProvider;
        }

        // VLC as fallback
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && ApplicationUtils.IsVlcSupportedVideoFormat(extension)
            && TryGetProvider<VlcVideoSupportProvider>(availableProviders, out IVideoSupportProvider vlcProvider))
        {
            return vlcProvider;
        }

        return null;
    }

    private static bool TryGetProvider<T>(IVideoSupportProvider[] availableProviders, out IVideoSupportProvider provider) where T : IVideoSupportProvider
    {
        provider = availableProviders.OfType<T>().FirstOrDefault();
        return provider != null;
    }
}
