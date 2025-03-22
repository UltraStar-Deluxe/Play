public class CompanionClientConnectionChangedEvent
{
    public bool IsConnected { get; private set; }
    public ICompanionClientHandler CompanionClientHandler { get; private set; }

    public CompanionClientConnectionChangedEvent(ICompanionClientHandler companionClientHandler, bool isConnected)
    {
        this.CompanionClientHandler = companionClientHandler;
        this.IsConnected = isConnected;
    }
}
