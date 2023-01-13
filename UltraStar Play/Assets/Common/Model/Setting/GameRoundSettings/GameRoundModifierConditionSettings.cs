public class GameRoundModifierConditionSettings
{
    public EGameRoundModifierCondition Condition { get; set; } = EGameRoundModifierCondition.Always;
    public int TimeFrom { get; set; }
    public int TimeUntil { get; set; } = 100;
    public int ScoreFrom { get; set; }
    public int ScoreUntil { get; set; } = 10000;

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

        return Condition == other.Condition
               && TimeFrom == other.TimeFrom
               && TimeUntil == other.TimeUntil
               && ScoreFrom == other.ScoreFrom
               && ScoreUntil == other.ScoreUntil;
    }

    public void CopyValues(GameRoundModifierConditionSettings other)
    {
        Condition = other.Condition;
        TimeFrom = other.TimeFrom;
        TimeUntil = other.TimeUntil;
        ScoreFrom = other.ScoreFrom;
        ScoreUntil = other.ScoreUntil;
    }
}
