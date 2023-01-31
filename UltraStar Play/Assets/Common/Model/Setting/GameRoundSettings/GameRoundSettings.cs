using System;
using System.Collections.Generic;

[Serializable]
public class GameRoundSettings
{
    public GameRoundFinishConditionSettings finishConditionSettings = new();
    public HashSet<EGameRoundModifier> modifiers = new();
    public GameRoundModifierConditionSettings modifierConditionSettings = new();

    public void CopyValues(GameRoundSettings other)
    {
        finishConditionSettings.CopyValues(other.finishConditionSettings);
        modifiers = new(other.modifiers);
        modifierConditionSettings.CopyValues(other.modifierConditionSettings);
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

        return finishConditionSettings.EqualsOther(other.finishConditionSettings)
               && modifiers.SetEquals(other.modifiers)
               && modifierConditionSettings.EqualsOther(other.modifierConditionSettings);
    }
}
