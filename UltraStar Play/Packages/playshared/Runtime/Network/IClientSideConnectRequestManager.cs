public interface IClientSideConnectRequestManager
{
    public bool TryGetConnectedServerHandler(out IConnectedServerHandler connectedServerHandler);
    public void RemoveConnectedServerHandler(IConnectedServerHandler connectedServerHandler);
}
