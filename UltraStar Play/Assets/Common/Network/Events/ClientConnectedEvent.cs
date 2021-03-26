public class ClientConnectedEvent
{
    public ConnectedClientHandler ConnectedClientHandler { get; private set; }

    public ClientConnectedEvent(ConnectedClientHandler connectedClientHandler)
    {
        this.ConnectedClientHandler = connectedClientHandler;
    }
}
