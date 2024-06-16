namespace CommonOnlineMultiplayer
{
    /**
     * Payload of the request for joining an online game.
     */
    public class LobbyConnectionRequestDto : JsonSerializable
    {
        public string DisplayName {get; private set; }

        public LobbyConnectionRequestDto()
        {
        }

        public LobbyConnectionRequestDto(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
