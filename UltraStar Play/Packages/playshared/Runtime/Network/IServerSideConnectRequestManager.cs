public interface IServerSideConnectRequestManager
{
    public bool TryGetConnectedClientHandler(string clientId, out IConnectedClientHandler connectedClientHandler);
}
