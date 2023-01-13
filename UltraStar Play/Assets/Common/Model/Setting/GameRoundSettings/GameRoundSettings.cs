using System;
using System.Collections.Generic;

public class GameRoundSettings
{
    public GameRoundFinishConditionSettings FinishConditionSettings { get; set; } = new();
    public HashSet<EGameRoundModifier> ModifierSettings { get; set; } = new();
    public GameRoundModifierConditionSettings ModifierConditionSettings { get; set; } = new();

    public GameRoundSettings()
    {
    }

    public GameRoundSettings(GameRoundSettings other)
    {
        FinishConditionSettings = new(other.FinishConditionSettings);
        ModifierSettings = new(other.ModifierSettings);
        ModifierConditionSettings = new(other.ModifierConditionSettings);
    }

    public bool EqualsOther(GameRoundSettings other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FinishConditionSettings.EqualsOther(other.FinishConditionSettings)
               && ModifierSettings.SetEquals(other.ModifierSettings)
               && ModifierConditionSettings.EqualsOther(other.ModifierConditionSettings);
    }

    public GameRoundSettings Clone()
    {
        return new(this);
    }
}
