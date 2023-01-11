using System.Collections.Generic;

public class GameRoundSettings
{
    public GameRoundFinishConditionSettings FinishConditionSettings { get; set; } = new();
    public HashSet<EGameRoundModifier> ModifierSettings { get; set; } = new();
    public GameRoundModifierConditionSettings ModifierConditionSettings { get; set; } = new();
}
