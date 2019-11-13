
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Settings
{
    public GameSettings GameSettings { get; set; } = new GameSettings();
    public GraphicSettings GraphicSettings { get; set; } = new GraphicSettings();
    public List<PlayerProfile> PlayerProfiles { get; set; } = CreateDefaultPlayerProfiles();

    private static List<PlayerProfile> CreateDefaultPlayerProfiles()
    {
        List<PlayerProfile> result = new List<PlayerProfile>();
        result.Add(new PlayerProfile("Player01", "Mikrofon (2- USB Microphone)", Difficulty.Medium, EAvatar.GenericPlayer01));
        result.Add(new PlayerProfile("Player02", "", Difficulty.Easy, EAvatar.GenericPlayer02));
        return result;
    }
}