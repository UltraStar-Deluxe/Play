public class ClientConnectionChangedEvent
{
    public bool IsConnected { get; private set; }
    public IConnectedClientHandler ConnectedClientHandler { get; private set; }

    public ClientConnectionChangedEvent(IConnectedClientHandler connectedClientHandler, bool isConnected)
    {
        this.ConnectedClientHandler = connectedClientHandler;
        this.IsConnected = isConnected;
    }
}
