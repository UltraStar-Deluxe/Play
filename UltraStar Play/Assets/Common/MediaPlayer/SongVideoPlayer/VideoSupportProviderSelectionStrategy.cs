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
        if (WebViewUtils.CanHandleWebViewUrl(videoUri))
        {
            return GetByType<WebViewVideoSupportProvider>(availableProviders);
        }

        string extension = Path.GetExtension(videoUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(videoUri);
        
        // Unity VideoPlayer is default, also for HTTP URLs
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && (ApplicationUtils.IsUnitySupportedVideoFormat(extension) || isHttp))
        {
            return GetByType<VideoPlayerVideoSupportProvider>(availableProviders);
        }
        
        // When audio and video are the same file, reuse the audio provider from VLC respectively AVPro.
        if (videoEqualsAudio)
        {
            if (songAudioPlayer.CurrentAudioSupportProvider is AvproAudioSupportProvider)
            {
                return GetByType<SongAudioPlayerAvproVideoSupportProvider>(availableProviders);
            }
            
            if (songAudioPlayer.CurrentAudioSupportProvider is VlcAudioSupportProvider)
            {
                return GetByType<SongAudioPlayerVlcVideoSupportProvider>(availableProviders);
            }
        }

        // AVPro as fallback
        if (settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && ApplicationUtils.IsAvproSupportedVideoFormat(extension))
        {
            return GetByType<AvproVideoSupportProvider>(availableProviders);
        }

        // VLC as fallback
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && ApplicationUtils.IsVlcSupportedVideoFormat(extension))
        {
            return GetByType<VlcVideoSupportProvider>(availableProviders);
        }

        return null;
    }

    private static IVideoSupportProvider GetByType<T>(IVideoSupportProvider[] availableProviders) where T : IVideoSupportProvider
    {
        return availableProviders.OfType<T>().FirstOrDefault();
    }
}
