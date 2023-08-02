using System.Collections.Generic;
using System.Linq;
using PortAudioForUnity;

public static class PortAudioConversionUtils
{
    public static HostApi ConvertHostApi(PortAudioHostApi portAudioHostApi)
    {
        if (portAudioHostApi == PortAudioHostApi.Default)
        {
            return ConvertHostApi(GetDefaultHostApi());
        }
        else
        {
            return (HostApi)portAudioHostApi;
        }
    }

    public static PortAudioHostApi ConvertHostApi(HostApi hostApi)
    {
        return (PortAudioHostApi)hostApi;
    }

    public static PortAudioHostApi GetDefaultHostApi()
    {
        HostApi defaultHostApi;

        // See http://files.portaudio.com/docs/v19-doxydocs/api_overview.html
        List<HostApi> availableHostApis = PortAudioUtils.HostApis;
        List<HostApi> preferredHostApis = new()
        {
            // Windows Host APIs
            HostApi.WASAPI,
            HostApi.DirectSound,
            HostApi.MME,
            HostApi.ASIO,

            // MacOS Host APIs
            HostApi.CoreAudio,

            // Linux Host APIs
            HostApi.ALSA,
            HostApi.JACK,
            HostApi.OSS,
            HostApi.AudioScienceHPI,
        };
        List<HostApi> availablePreferredHostApis = preferredHostApis
            .Intersect(availableHostApis)
            .ToList();
        if (availablePreferredHostApis.IsNullOrEmpty())
        {
            defaultHostApi = PortAudioUtils.DefaultHostApiInfo.HostApi;
        }
        else
        {
            defaultHostApi = availablePreferredHostApis.FirstOrDefault();
        }

        return ConvertHostApi(defaultHostApi);
    }
}
