public struct AchievementId
{
    public static readonly AchievementId completeSong = new("COMPLETE_A_SONG");
    public static readonly AchievementId completeSongWithVocalsVolumeZero = new("COMPLETE_A_SONG_WITH_VOCALS_VOLUME_ZERO");
    public static readonly AchievementId editNotesInSongEditor = new("EDIT_NOTES_IN_SONG_EDITOR");
    public static readonly AchievementId startDuetWithDifferentLyrics = new("START_DUET_WITH_DIFFERENT_LYRICS");
    public static readonly AchievementId startSongWithFourOrMorePlayers = new("START_A_SONG_WITH_FOUR_OR_MORE_PLAYERS");
    public static readonly AchievementId startMedleyWithAtLeastTwoSongs = new("START_MEDLEY_WITH_AT_LEAST_TWO_SONGS");
    public static readonly AchievementId pauseSingingAfterOneMinute = new("PAUSE_SINGING_AFTER_ONE_MINUTE");
    public static readonly AchievementId useWebcamInSingScene = new("USE_WEBCAM_IN_SING_SCENE");
    public static readonly AchievementId disconnectCompanionAppWhenSinging = new("DISCONNECT_COMPANION_APP_WHEN_SINGING");
    public static readonly AchievementId getMoreThan9000Points = new("GET_MORE_THAN_9000_POINTS");
    public static readonly AchievementId showFinalTeamResults = new("SHOW_FINAL_TEAM_RESULTS");
    public static readonly AchievementId watchCreditsWithoutSkipping = new("WATCH_CREDITS_WITHOUT_SKIPPING");
    public static readonly AchievementId browseMoreThan100Songs = new("BROWSE_MORE_THAN_100_SONGS");
    public static readonly AchievementId completeMoreThan10SongsInARow = new("COMPLETE_MORE_THAN_10_SONGS_IN_A_ROW");
    public static readonly AchievementId getMoreThan10PerfectRatingsInASong = new("GET_MORE_THAN_10_PERFECT_RATINGS_IN_A_SONG");

    public string Id { get; private set; }
    
    private AchievementId(string id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return $"AchievementId('{Id}')";
    }
}
