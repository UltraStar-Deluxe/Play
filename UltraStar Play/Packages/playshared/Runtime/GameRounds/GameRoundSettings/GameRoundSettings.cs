using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class GameRoundSettings
{
    public GameRoundFinishConditionSettings finishConditionSettings = new();
    public HashSet<EGameRoundModifier> modifiers = new();
    public GameRoundModifierConditionSettings modifierConditionSettings = new();

    public HashSet<EGameRoundModifier> UnconditionalModifiers => modifiers
        .Where(it => it is EGameRoundModifier.ShortSong or EGameRoundModifier.PassTheMic)
        .ToHashSet();
    
    public HashSet<EGameRoundModifier> ConditionalModifiers => modifiers
        .Except(UnconditionalModifiers)
        .ToHashSet();

    public GameRoundSettings()
    {
    }
    
    public GameRoundSettings(GameRoundSettings other)
    {
        CopyValues(other);
    }
    
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
