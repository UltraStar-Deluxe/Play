using System;

public class GameRoundFinishConditionSettings
{
    public EGameRoundFinishCondition Condition { get; set; } = EGameRoundFinishCondition.ReachEndOfSong;
    public int Points { get; set; } = 3000;

    public GameRoundFinishConditionSettings()
    {
    }

    public GameRoundFinishConditionSettings(GameRoundFinishConditionSettings other)
    {
        Condition = other.Condition;
        Points = other.Points;
    }

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
}
