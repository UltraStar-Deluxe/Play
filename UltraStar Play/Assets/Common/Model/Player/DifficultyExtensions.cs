using UnityEngine;

public static class DifficultyExtensions
{
    public static string GetTranslatedName(this EDifficulty difficulty)
    {
        string i18nCode = difficulty.GetI18NCode();
        return I18NManager.Instance.GetTranslation(i18nCode);
    }

    private static string GetI18NCode(this EDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EDifficulty.Easy: return "difficulty.easy";
            case EDifficulty.Medium: return "difficulty.medium";
            case EDifficulty.Hard: return "difficulty.hard";
            default:
                throw new UnityException("Unhandled difficulty: " + difficulty);
        }
    }

    public static int GetRoundingDistance(this EDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EDifficulty.Easy: return 2;
            case EDifficulty.Medium: return 1;
            case EDifficulty.Hard: return 0;
            default:
                throw new UnityException("Unhandled difficulty: " + difficulty);
        }
    }
}