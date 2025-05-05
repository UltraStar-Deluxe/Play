using System.Linq;

public static class MicProfileExtensions
{
    public static bool IsConnected(this MicProfile micProfile, IServerSideCompanionClientManager serverSideCompanionClientManager)
    {
        return (micProfile.IsInputFromConnectedClient && serverSideCompanionClientManager.TryGet(micProfile.ConnectedClientId, out ICompanionClientHandler _))
               || (!micProfile.IsInputFromConnectedClient
                   && IMicrophoneAdapter.Instance.Devices.Contains(micProfile.Name)
                   && (micProfile.ChannelIndex == 0 || IMicrophoneAdapter.Instance.UsePortAudio));
    }

    public static bool IsEnabledAndConnected(this MicProfile micProfile, IServerSideCompanionClientManager serverSideCompanionClientManager)
    {
        return micProfile.IsEnabled && micProfile.IsConnected(serverSideCompanionClientManager);
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

            if (ThreadUtils.IsMainThread())
            {
                // Add channel to mic profile name only if the device is connected and has more than one channel.
                if (IMicrophoneAdapter.Instance.Devices.Contains(micProfile.Name))
                {
                    IMicrophoneAdapter.Instance.GetDeviceCaps(micProfile.Name, out int minSampleRate, out int maxSampleRate, out int channelCount);
                    if (channelCount > 1)
                    {
                        return $"{micProfile.Name} - Channel {micProfile.ChannelIndex}";
                    }
                }
            }
            else
            {
                // Microphone.devices can only be called from the main thread.
                // Thus, assume this is debug output and add the channel to the result.
                return $"{micProfile.Name} - Channel {micProfile.ChannelIndex}";
            }
        }

        return micProfile.Name;
    }
}
