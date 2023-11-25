namespace CommonOnlineMultiplayer
{
    public enum ENetcodeMessageType
    {
        Other,

        CurrentLobbyMembersRequest,
        CurrentLobbyMembersResponse,

        PlayerHasSongLocallyRequest,
        PlayerHasSongLocallyResponse,

        SuggestSongRequest,

        StartSongRequest,

        BeatAnalyzedEventRequest,
    }
}
