using System.Linq;
using PortAudioForUnity;

public static class MicProfileExtensions
{
    public static bool IsConnected(this MicProfile micProfile, IServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        return (micProfile.IsInputFromConnectedClient && serverSideConnectRequestManager.TryGetConnectedClientHandler(micProfile.ConnectedClientId, out IConnectedClientHandler _))
               || (!micProfile.IsInputFromConnectedClient
                   && MicrophoneAdapter.Devices.Contains(micProfile.Name)
                   && (micProfile.ChannelIndex == 0 || MicrophoneAdapter.UsePortAudio));
    }

    public static bool IsEnabledAndConnected(this MicProfile micProfile, IServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        return micProfile.IsEnabled && micProfile.IsConnected(serverSideConnectRequestManager);
    }

    public static string GetDisplayNameWithChannel(this MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return "";
        }

        if (!micProfile.IsInputFromConnectedClient)
        {
            if (micProfile.ChannelIndex > 0)
            {
                return $"{micProfile.Name} - Channel {micProfile.ChannelIndex}";
            }

            // Add channel to mic profile name only if the device is connected and has more than one channel.
            if (MicrophoneAdapter.Devices.Contains(micProfile.Name))
            {
                MicrophoneAdapter.GetDeviceCaps(micProfile.Name, out int minSampleRate, out int maxSampleRate, out int channelCount);
                if (channelCount > 1)
                {
                    return $"{micProfile.Name} - Channel {micProfile.ChannelIndex}";
                }
            }
        }

        return micProfile.Name;
    }
}
