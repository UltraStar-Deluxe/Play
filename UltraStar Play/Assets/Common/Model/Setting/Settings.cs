
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Settings
{
    public GameSettings GameSettings { get; set; } = new GameSettings();
    public GraphicSettings GraphicSettings { get; set; } = new GraphicSettings();
    public List<PlayerProfile> PlayerProfiles { get; set; } = CreateDefaultPlayerProfiles();
    public List<MicProfile> MicProfiles { get; set; } = new List<MicProfile>();

    public SongEditorSettings SongEditorSettings { get; set; } = new SongEditorSettings();

    private static List<PlayerProfile> CreateDefaultPlayerProfiles()
    {
        List<PlayerProfile> result = new List<PlayerProfile>();
        result.Add(new PlayerProfile("Player01", EDifficulty.Medium, EAvatar.GenericPlayer01));
        result.Add(new PlayerProfile("Player02", EDifficulty.Easy, EAvatar.GenericPlayer02));
        return result;
    }
}