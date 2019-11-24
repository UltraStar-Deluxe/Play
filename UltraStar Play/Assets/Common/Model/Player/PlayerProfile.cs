using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile : IEquatable<PlayerProfile>
{
    public string Name { get; set; } = "New Player";
    public EDifficulty Difficulty { get; set; } = EDifficulty.Medium;
    public EAvatar Avatar { get; set; } = EAvatar.GenericPlayer01;
    public bool IsEnabled { get; set; } = true;

    public PlayerProfile()
    {
    }

    public PlayerProfile(string name, EDifficulty difficulty, EAvatar avatar)
    {
        this.Name = name;
        this.Difficulty = difficulty;
        this.Avatar = avatar;
    }

    public bool Equals(PlayerProfile other)
    {
        // TODO: Use EqualsBuilder or something similar for C#
        return Name == other.Name && Difficulty == other.Difficulty && Avatar == other.Avatar && IsEnabled == other.IsEnabled;
    }

    public override bool Equals(object obj)
    {
        if (obj is PlayerProfile)
        {
            return Equals(obj as PlayerProfile);
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        // TODO: Use HashCodeBuilder or something similar for C#
        return Name.GetHashCode();
    }
}
