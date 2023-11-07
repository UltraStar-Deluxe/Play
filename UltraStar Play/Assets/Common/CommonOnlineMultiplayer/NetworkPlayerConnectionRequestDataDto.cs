namespace CommonOnlineMultiplayer
{
    public class NetworkPlayerConnectionRequestDataDto : JsonSerializable
    {
        public ulong SteamId { get; private set; }
        public string DisplayName {get; private set; }

        public NetworkPlayerConnectionRequestDataDto()
        {
        }

        public NetworkPlayerConnectionRequestDataDto(
            ulong steamId,
            string displayName)
        {
            SteamId = steamId;
            DisplayName = displayName;
        }
    }
}
