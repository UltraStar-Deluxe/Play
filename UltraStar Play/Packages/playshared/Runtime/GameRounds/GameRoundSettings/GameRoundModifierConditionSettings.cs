using System;

[Serializable]
public class GameRoundModifierConditionSettings
{
    public EGameRoundModifierCondition condition = EGameRoundModifierCondition.Always;

    /**
     * Time from in percent (0 to 100)
     */
    public int timeFrom;

    /**
     * Time until in percent (0 to 100)
     */
    public int timeUntil = 100;

    /**
     * Score from (0 to 10000)
     */
    public int scoreFrom;

    /**
     * Score until (0 to 10000)
     */
    public int scoreUntil = 10000;

    public bool EqualsOther(GameRoundModifierConditionSettings other)
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
               && timeFrom == other.timeFrom
               && timeUntil == other.timeUntil
               && scoreFrom == other.scoreFrom
               && scoreUntil == other.scoreUntil;
    }

    public void CopyValues(GameRoundModifierConditionSettings other)
    {
        condition = other.condition;
        timeFrom = other.timeFrom;
        timeUntil = other.timeUntil;
        scoreFrom = other.scoreFrom;
        scoreUntil = other.scoreUntil;
    }
}
