using System;

[Serializable]
public class GameRoundFinishConditionSettings
{
    public EGameRoundFinishCondition condition = EGameRoundFinishCondition.ReachEndOfSong;
    public int points = 3000;

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

        return condition == other.condition
               && points == other.points;
    }

    public void CopyValues(GameRoundFinishConditionSettings other)
    {
        condition = other.condition;
        points = other.points;
    }
}
