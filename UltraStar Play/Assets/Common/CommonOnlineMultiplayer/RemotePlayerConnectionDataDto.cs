namespace CommonOnlineMultiplayer
{
    public class RemotePlayerConnectionDataDto : JsonSerializable
    {
        public ulong UnityNetcodeClientId { get; private set; }
        public ulong SteamId { get; private set; }
        public string ConnectionGuid { get; private set; }
        public string DisplayName {get; private set; }
        public string CurrentSceneName {get; private set; }

        public RemotePlayerConnectionDataDto()
        {
        }

        public RemotePlayerConnectionDataDto(
            ulong unityNetcodeClientId,
            ulong steamId,
            string connectionGuid,
            string displayName,
            string currentSceneName)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
            SteamId = steamId;
            ConnectionGuid = connectionGuid;
            DisplayName = displayName;
            CurrentSceneName = currentSceneName;
        }
    }
}
