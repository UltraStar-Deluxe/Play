using System;

public class GameRoundFinishConditionSettings
{
    public EGameRoundFinishCondition Condition { get; set; } = EGameRoundFinishCondition.ReachEndOfSong;
    public int Points { get; set; } = 3000;

    public bool EqualsOther(GameRoundFinishConditionSettings other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Condition == other.Condition
               && Points == other.Points;
    }

    public void CopyValues(GameRoundFinishConditionSettings other)
    {
        Condition = other.Condition;
        Points = other.Points;
    }
}
