namespace CommonOnlineMultiplayer
{
    public enum ENetcodeMessageType
    {
        Other,

        CurrentLobbyMembersRequest,
        CurrentLobbyMembersResponse,

        PlayerHasSongLocallyRequest,
        PlayerHasSongLocallyResponse,

        SingSceneReadyRequest,
        SingSceneReadyResponse,

        PauseRequest,
        UnpauseRequest,

        SuggestSongRequest,

        StartSongRequest,
        EndSingSceneRequest,
        AbortSingSceneRequest,

        BeatAnalyzedEventRequest,
    }
}
