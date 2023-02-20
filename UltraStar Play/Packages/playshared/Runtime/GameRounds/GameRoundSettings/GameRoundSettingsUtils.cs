using System.Collections.Generic;

public static class GameRoundSettingsUtils
{
    public static string GetFinishConditionDescription(GameRoundSettings gameRoundSettings)
    {
        if (gameRoundSettings.finishConditionSettings.condition == EGameRoundFinishCondition.ReachPoints)
        {
            return $"Reach {gameRoundSettings.finishConditionSettings.points} points";
        }
        else if (gameRoundSettings.finishConditionSettings.condition == EGameRoundFinishCondition.ReachAdvanceOfPoints)
        {
            return $"Reach advance of {gameRoundSettings.finishConditionSettings.points} points";
        }
        
        return "";
    }

    public static string GetModifierConditionDescription(GameRoundSettings gameRoundSettings)
    {
        HashSet<EGameRoundModifier> modifiers = gameRoundSettings.modifiers;
        GameRoundModifierConditionSettings modifierConditionSettings = gameRoundSettings.modifierConditionSettings;
        if (modifiers.IsNullOrEmpty()
            || modifierConditionSettings == null
            || modifierConditionSettings.condition == EGameRoundModifierCondition.Always)
        {
            return "";
        }
        else if (modifierConditionSettings.condition == EGameRoundModifierCondition.PlayerAdvance)
        {
            return $"when player has advance of {modifierConditionSettings.scoreFrom} points";
        }
        else if (modifierConditionSettings.condition == EGameRoundModifierCondition.ScoreRange)
        {
            return $"when score is between {modifierConditionSettings.scoreFrom} and {modifierConditionSettings.scoreUntil}";
        }
        else if (modifierConditionSettings.condition == EGameRoundModifierCondition.TimeRange)
        {
            return $"when time is between {modifierConditionSettings.timeFrom}% and {modifierConditionSettings.timeUntil}%";
        }

        return "";
    }
}
