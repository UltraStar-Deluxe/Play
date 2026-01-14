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
        if (WebViewUtils.CanHandleWebViewUrl(audioUri)
            && TryGetProvider<WebViewAudioSupportProvider>(availableProviders, out IAudioSupportProvider webViewProvider))
        {
            return webViewProvider;
        }

        string extension = Path.GetExtension(audioUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(audioUri);

        // Unity API is default, also for HTTP URLs
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always)
        {
            if (ApplicationUtils.IsSupportedMidiFormat(extension)
                && TryGetProvider<MidiAudioSupportProvider>(availableProviders, out IAudioSupportProvider midiProvider))
            {
                return midiProvider;
            }

            if (ApplicationUtils.IsUnitySupportedVideoFormat(extension)
                && TryGetProvider<VideoPlayerAudioSupportProvider>(availableProviders, out IAudioSupportProvider videoPlayerProvider))
            {
                return videoPlayerProvider;
            }

            if ((ApplicationUtils.IsUnitySupportedAudioFormat(extension) || isHttp)
                && TryGetProvider<AudioSourceAudioSupportProvider>(availableProviders, out IAudioSupportProvider audioSourceProvider))
            {
                return audioSourceProvider;
            }
        }

        // AVPro as fallback
        if (settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsAvproSupportedAudioFormat(extension)
                || ApplicationUtils.IsAvproSupportedVideoFormat(extension))
            && TryGetProvider<AvproAudioSupportProvider>(availableProviders, out IAudioSupportProvider avproProvider))
        {
            return avproProvider;
        }

        // VLC as fallback
        if (settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsVlcSupportedAudioFormat(extension)
                || ApplicationUtils.IsVlcSupportedVideoFormat(extension))
            && TryGetProvider<VlcAudioSupportProvider>(availableProviders, out IAudioSupportProvider vlcProvider))
        {
            return vlcProvider;
        }

        return null;
    }

    private static bool TryGetProvider<T>(IAudioSupportProvider[] availableProviders,
        out IAudioSupportProvider provider) where T : IAudioSupportProvider
    {
        provider = availableProviders.OfType<T>().FirstOrDefault();
        return provider != null;
    }
}
