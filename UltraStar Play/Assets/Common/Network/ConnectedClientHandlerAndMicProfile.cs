public class ConnectedClientHandlerAndMicProfile
{
    public IConnectedClientHandler ConnectedClientHandler { get; private set; }
    public MicProfile MicProfile { get; private set; }

    public ConnectedClientHandlerAndMicProfile(IConnectedClientHandler connectedClientHandler, MicProfile micProfile)
    {
        ConnectedClientHandler = connectedClientHandler;
        MicProfile = micProfile;
    }
}
