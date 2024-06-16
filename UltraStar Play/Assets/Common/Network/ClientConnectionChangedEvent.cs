public class ClientConnectionChangedEvent
{
    public bool IsConnected { get; private set; }
    public ICompanionClientHandler CompanionClientHandler { get; private set; }

    public ClientConnectionChangedEvent(ICompanionClientHandler companionClientHandler, bool isConnected)
    {
        this.CompanionClientHandler = companionClientHandler;
        this.IsConnected = isConnected;
    }
}
