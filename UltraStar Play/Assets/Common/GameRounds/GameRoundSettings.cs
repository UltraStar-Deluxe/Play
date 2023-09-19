using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class GameRoundSettings
{
    public List<IGameRoundModifier> modifiers = new();

    public bool AnyModifierActive
    {
        get
        {
            return !modifiers.IsNullOrEmpty();
        }
    }

    public GameRoundSettings()
    {
    }

    public GameRoundSettings(GameRoundSettings other)
    {
        CopyValues(other);
    }

    public void CopyValues(GameRoundSettings other)
    {
        modifiers = new(other.modifiers);
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

        return modifiers.SequenceEqual(other.modifiers);
    }
}
