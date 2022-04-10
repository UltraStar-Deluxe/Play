using System.Linq;
using UnityEngine;

public static class MicProfileExtensions
{
    public static bool IsConnected(this MicProfile micProfile, IServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        return (micProfile.IsInputFromConnectedClient && serverSideConnectRequestManager.TryGetConnectedClientHandler(micProfile.ConnectedClientId, out IConnectedClientHandler _))
               || (!micProfile.IsInputFromConnectedClient && Microphone.devices.Contains(micProfile.Name));
    }

    public static bool IsEnabledAndConnected(this MicProfile micProfile, IServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        return micProfile.IsEnabled && micProfile.IsConnected(serverSideConnectRequestManager);
    }
}
