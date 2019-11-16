using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public string Name { get; set; } = "New Player";
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;
    public EAvatar Avatar { get; set; } = EAvatar.GenericPlayer01;
    public bool IsEnabled { get; set; } = true;

    public PlayerProfile()
    {
    }

    public PlayerProfile(string name, Difficulty difficulty, EAvatar avatar)
    {
        this.Name = name;
        this.Difficulty = difficulty;
        this.Avatar = avatar;
    }
}
