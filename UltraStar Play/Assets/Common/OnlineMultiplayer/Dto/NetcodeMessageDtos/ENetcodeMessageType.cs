namespace CommonOnlineMultiplayer
{
    public enum ENetcodeMessageType
    {
        Other,

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

        SingingResultsPlayerScoreRequest,
    }
}
