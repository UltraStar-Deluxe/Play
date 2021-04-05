public class ClientConnectionEvent
{
    public bool IsConnected { get; private set; }
    public ConnectedClientHandler ConnectedClientHandler { get; private set; }

    public ClientConnectionEvent(ConnectedClientHandler connectedClientHandler, bool isConnected)
    {
        this.ConnectedClientHandler = connectedClientHandler;
        this.IsConnected = isConnected;
    }
}
