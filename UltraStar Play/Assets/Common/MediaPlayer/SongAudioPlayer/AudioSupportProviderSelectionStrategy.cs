using System.Collections.Generic;
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
        if (TryGetWebViewProvider(availableProviders, audioUri, out IAudioSupportProvider webViewProvider))
        {
            return webViewProvider;
        }

        // MIDI files
        if (TryGetMidiProvider(availableProviders, audioUri, out IAudioSupportProvider provider))
        {
            return provider;
        }

        // Select the audio support provider of the API that has highest priority
        List<EMediaApi> mediaApis = MediaApiPriorityUtils.GetMediaApisOrderedByPriority(settings);
        foreach (EMediaApi mediaApi in mediaApis)
        {
            if (mediaApi == EMediaApi.Unity
                && TryGetUnityProvider(availableProviders, audioUri, out provider))
            {
                return provider;
            }

            if (mediaApi == EMediaApi.Avpro
                && TryGetAvProProvider(availableProviders, audioUri, out provider))
            {
                return provider;
            }

            if (mediaApi == EMediaApi.Vlc
                && TryGetVlcProvider(availableProviders, audioUri, out provider))
            {
                return provider;
            }
        }

        return null;
    }

    private static bool TryGetWebViewProvider(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        out IAudioSupportProvider provider)
    {
        if (WebViewUtils.CanHandleWebViewUrl(audioUri)
            && TryGetProvider<WebViewAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    private static bool TryGetMidiProvider(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        out IAudioSupportProvider provider)
    {
        string extension = Path.GetExtension(audioUri);
        
        if (ApplicationUtils.IsSupportedMidiFormat(extension)
            && TryGetProvider<MidiAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    private static bool TryGetVlcProvider(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        out IAudioSupportProvider provider)
    {
        string extension = Path.GetExtension(audioUri);
        
        if ((ApplicationUtils.IsVlcSupportedAudioFormat(extension)
             || ApplicationUtils.IsVlcSupportedVideoFormat(extension))
            && TryGetProvider<VlcAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    private static bool TryGetAvProProvider(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        out IAudioSupportProvider provider)
    {
        string extension = Path.GetExtension(audioUri);
        
        if ((ApplicationUtils.IsAvproSupportedAudioFormat(extension)
             || ApplicationUtils.IsAvproSupportedVideoFormat(extension))
            && TryGetProvider<AvproAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    private static bool TryGetUnityProvider(
        IAudioSupportProvider[] availableProviders,
        string audioUri,
        out IAudioSupportProvider provider)
    {
        string extension = Path.GetExtension(audioUri);
        bool isHttp = WebRequestUtils.IsHttpOrHttpsUri(audioUri);
        
        if (ApplicationUtils.IsUnitySupportedVideoFormat(extension)
            && TryGetProvider<VideoPlayerAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        if ((ApplicationUtils.IsUnitySupportedAudioFormat(extension) || isHttp)
            && TryGetProvider<AudioSourceAudioSupportProvider>(availableProviders, out provider))
        {
            return true;
        }

        provider = null;
        return false;
    }

    private static bool TryGetProvider<T>(IAudioSupportProvider[] availableProviders, out IAudioSupportProvider provider) where T : IAudioSupportProvider
    {
        provider = availableProviders.OfType<T>().FirstOrDefault();
        return provider != null;
    }
}
