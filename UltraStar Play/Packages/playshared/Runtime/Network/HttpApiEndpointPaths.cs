public static class HttpApiEndpointPaths
{
    public const string AvailableMicrophones = "api/rest/availableMicrophones";
    public const string AvailablePlayers = "api/rest/availablePlayers";
    public const string Config = "api/rest/config";
    public const string Endpoints = "api/rest/endpoints";
    public const string Hello = "api/rest/hello/{name}";
    public const string Input = "api/rest/input/{inputControl}";
    public const string InputMouseDelta = "api/rest/input/mouseDelta/{deltaX}/{deltaY}";
    public const string InputScrollWheel = "api/rest/input/scrollWheel/{deltaX}/{deltaY}";
    public const string Language = "api/rest/language";
    public const string PlaylistFavorites = "api/rest/playlist/favorites";
    public const string PlaylistFavoritesEntry = "api/rest/playlist/favorites/entry/{songId}";
    public const string Song = "api/rest/song/{songId}";
    public const string SongImage = "api/rest/song/{songId}/image";
    public const string SongQueue = "api/rest/songQueue";
    public const string SongQueueEntry = "api/rest/songQueue/entry";
    public const string SongQueueEntryIndex = "api/rest/songQueue/entry/{index}";
    public const string Songs = "api/rest/songs";
    public const string Statistics = "api/rest/stats";
    public const string Translations = "api/rest/translations";
}
