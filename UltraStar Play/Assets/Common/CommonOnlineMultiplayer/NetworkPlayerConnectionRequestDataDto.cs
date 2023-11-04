namespace CommonOnlineMultiplayer
{
    public class NetworkPlayerConnectionRequestDataDto : JsonSerializable
    {
        public ulong SteamId { get; private set; }
        public string ConnectionGuid { get; private set; }
        public string DisplayName {get; private set; }
        public string CurrentSceneName {get; private set; }

        public NetworkPlayerConnectionRequestDataDto()
        {
        }

        public NetworkPlayerConnectionRequestDataDto(
            ulong steamId,
            string connectionGuid,
            string displayName,
            string currentSceneName)
        {
            SteamId = steamId;
            ConnectionGuid = connectionGuid;
            DisplayName = displayName;
            CurrentSceneName = currentSceneName;
        }
    }
}
