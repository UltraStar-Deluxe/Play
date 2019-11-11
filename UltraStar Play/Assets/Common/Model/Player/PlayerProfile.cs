using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public string Name { get; set; } = "New Player";
    public string MicDevice { get; set; } = "";
    public Difficulty Difficulty { get; private set; } = Difficulty.Medium;
    public Color Color { get; set; } = new Color(0, 0.8f, 0, 1);

    public PlayerProfile()
    {
    }

    public PlayerProfile(string name, string micDevice, Difficulty difficulty, Color color)
    {
        this.Name = name;
        this.MicDevice = micDevice;
        this.Difficulty = difficulty;
        this.Color = color;
    }
}
