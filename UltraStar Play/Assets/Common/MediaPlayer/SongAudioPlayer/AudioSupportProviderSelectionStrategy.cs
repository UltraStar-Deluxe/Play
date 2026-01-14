using System.IO;
using System.Linq;

public static class AudioSupportProviderSelectionStrategy
{
    public static IAudioSupportProvider Select(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        Settings settings)
    {
        if (availableProviders.IsNullOrEmpty())
        {
            return null;
        }

        // WebView URLs
        if (WebViewUtils.CanHandleWebViewUrl(audioUri))
        {
            return GetByType<WebViewAudioSupportProvider>(availableProviders);
        }

        string extension = Path.GetExtension(audioUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(audioUri);
        
        // Unity API is default, also for HTTP URLs
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always)
        {
            if (ApplicationUtils.IsSupportedMidiFormat(extension))
            {
                return GetByType<MidiAudioSupportProvider>(availableProviders);
            }
            
            if (ApplicationUtils.IsUnitySupportedVideoFormat(extension))
            {
                return GetByType<VideoPlayerAudioSupportProvider>(availableProviders);
            }
            if (ApplicationUtils.IsUnitySupportedAudioFormat(extension) || isHttp)
            {
                return GetByType<AudioSourceAudioSupportProvider>(availableProviders);
            }
        }

        // AVPro as fallback
        if (settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsAvproSupportedAudioFormat(extension)
                || ApplicationUtils.IsAvproSupportedVideoFormat(extension)))
        {
            return GetByType<AvproAudioSupportProvider>(availableProviders);
        }

        // VLC as fallback
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsVlcSupportedAudioFormat(extension)
                || ApplicationUtils.IsVlcSupportedVideoFormat(extension)))
        {
            return GetByType<VlcAudioSupportProvider>(availableProviders);
        }

        return null;
    }

    private static IAudioSupportProvider GetByType<T>(IAudioSupportProvider[] availableProviders) where T : IAudioSupportProvider
    {
        return availableProviders.OfType<T>().FirstOrDefault();
    }
}
