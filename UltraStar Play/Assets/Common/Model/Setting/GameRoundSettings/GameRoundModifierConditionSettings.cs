public class GameRoundModifierConditionSettings
{
    public EGameRoundModifierCondition Condition { get; set; }
    public int TimeFrom { get; set; }
    public int TimeUntil { get; set; } = 100;
    public int ScoreFrom { get; set; }
    public int ScoreUntil { get; set; } = 10000;
}
