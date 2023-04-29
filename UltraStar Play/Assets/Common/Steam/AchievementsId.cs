public struct AchievementId
{
    public static readonly AchievementId completeSong = new("COMPLETE_A_SONG");
    public static readonly AchievementId editNotesInSongEditor = new("EDIT_NOTES_IN_SONG_EDITOR");

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
