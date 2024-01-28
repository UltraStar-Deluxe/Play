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

        PauseGameRequest,
        UnpauseGameRequest,

        SuggestSongRequest,

        StartSongRequest,
        EndSingSceneRequest,
        AbortSingSceneRequest,

        BeatAnalyzedEventRequest,
    }
}
