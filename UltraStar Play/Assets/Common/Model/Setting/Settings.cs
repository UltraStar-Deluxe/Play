
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class Settings
{
    public GameSettings GameSettings { get; set; } = new GameSettings();
    public GraphicSettings GraphicSettings { get; set; } = new GraphicSettings();
    public AudioSettings AudioSettings { get; set; } = new AudioSettings();
    public List<PlayerProfile> PlayerProfiles { get; set; } = CreateDefaultPlayerProfiles();
    public List<MicProfile> MicProfiles { get; set; } = new List<MicProfile>();

    public SongEditorSettings SongEditorSettings { get; set; } = new SongEditorSettings();
    public SongSelectSettings SongSelectSettings { get; set; } = new SongSelectSettings();

    public DeveloperSettings DeveloperSettings { get; set; } = new DeveloperSettings();

    // The releases to be ignored when checking for updates.
    // When containing the string "all", then all releases will be ignored.
    public List<string> IgnoredReleases { get; set; } = new List<string>();

    private static List<PlayerProfile> CreateDefaultPlayerProfiles()
    {
        List<PlayerProfile> result = new List<PlayerProfile>();
        result.Add(new PlayerProfile("Player01", EDifficulty.Medium, EAvatar.GenericPlayer01));
        result.Add(new PlayerProfile("Player02", EDifficulty.Easy, EAvatar.GenericPlayer02));
        return result;
    }
}
