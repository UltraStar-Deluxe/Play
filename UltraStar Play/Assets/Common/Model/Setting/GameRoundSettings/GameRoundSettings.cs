using System;
using System.Collections.Generic;

public class GameRoundSettings
{
    public GameRoundFinishConditionSettings FinishConditionSettings { get; set; } = new();
    public HashSet<EGameRoundModifier> Modifiers { get; set; } = new();
    public GameRoundModifierConditionSettings ModifierConditionSettings { get; set; } = new();

    public void CopyValues(GameRoundSettings other)
    {
        FinishConditionSettings.CopyValues(other.FinishConditionSettings);
        Modifiers = new(other.Modifiers);
        ModifierConditionSettings.CopyValues(other.ModifierConditionSettings);
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
               && Modifiers.SetEquals(other.Modifiers)
               && ModifierConditionSettings.EqualsOther(other.ModifierConditionSettings);
    }
}
