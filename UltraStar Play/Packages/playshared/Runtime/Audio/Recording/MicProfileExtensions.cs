using System.Linq;
using PortAudioForUnity;
using UnityEngine;

public static class MicProfileExtensions
{
    public static bool IsConnected(this MicProfile micProfile, IServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        return (micProfile.IsInputFromConnectedClient && serverSideConnectRequestManager.TryGetConnectedClientHandler(micProfile.ConnectedClientId, out IConnectedClientHandler _))
               || (!micProfile.IsInputFromConnectedClient && MicrophoneAdapter.Devices.Contains(micProfile.Name));
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

        if (!micProfile.IsInputFromConnectedClient
            && MicrophoneAdapter.Devices.Contains(micProfile.Name))
        {
            // Add channel to mic profile name, if the device has more than one channel.
            MicrophoneAdapter.GetDeviceCaps(micProfile.Name, out int minSampleRate, out int maxSampleRate, out int channelCount);
            if (channelCount > 1)
            {
                return $"{micProfile.Name} - Channel {micProfile.ChannelIndex}";
            }
        }

        return micProfile.Name;
    }
}
