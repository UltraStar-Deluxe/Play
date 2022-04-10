public interface IServerSideConnectRequestManager
{
    public bool TryGetConnectedClientHandler(string clientIpEndPointId, out IConnectedClientHandler connectedClientHandler);
}
