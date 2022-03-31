
using System;
using System.Collections.Generic;

[Serializable]
public class Settings
{
    public GameSettings GameSettings { get; set; } = new();
    public GraphicSettings GraphicSettings { get; set; } = new();
    public AudioSettings AudioSettings { get; set; } = new();
    public List<PlayerProfile> PlayerProfiles { get; set; } = CreateDefaultPlayerProfiles();
    public List<MicProfile> MicProfiles { get; set; } = new();
    public string LastMicProfileNameInRecordingOptionsScene { get; set; }

    public SongEditorSettings SongEditorSettings { get; set; } = new();
    public SongSelectSettings SongSelectSettings { get; set; } = new();

    public DeveloperSettings DeveloperSettings { get; set; } = new();

    // The releases to be ignored when checking for updates.
    // When containing the string "all", then all releases will be ignored.
    public List<string> IgnoredReleases { get; set; } = new();

    private static List<PlayerProfile> CreateDefaultPlayerProfiles()
    {
        List<PlayerProfile> result = new();
        result.Add(new PlayerProfile("Player01", EDifficulty.Medium, EAvatar.GenericPlayer01));
        result.Add(new PlayerProfile("Player02", EDifficulty.Easy, EAvatar.GenericPlayer02));
        return result;
    }
}
