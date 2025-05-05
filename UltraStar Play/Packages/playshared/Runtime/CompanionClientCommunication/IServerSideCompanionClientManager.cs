public interface IServerSideCompanionClientManager
{
    public bool TryGet(string clientId, out ICompanionClientHandler companionClientHandler);
}
