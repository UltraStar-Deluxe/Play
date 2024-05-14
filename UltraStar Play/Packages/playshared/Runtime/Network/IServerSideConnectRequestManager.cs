public interface IServerSideConnectRequestManager
{
    public bool TryGetCompanionClientHandler(string clientId, out ICompanionClientHandler companionClientHandler);
}
