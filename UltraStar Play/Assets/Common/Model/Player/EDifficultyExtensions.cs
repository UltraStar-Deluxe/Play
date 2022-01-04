using UnityEngine;
using ProTrans;

public static class EDifficultyExtensions
{
    public static string GetTranslatedName(this EDifficulty difficulty)
    {
        string i18nCode = difficulty.GetI18NCode();
        return TranslationManager.GetTranslation(i18nCode);
    }

    private static string GetI18NCode(this EDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EDifficulty.Easy: return R.Messages.difficulty_easy;
            case EDifficulty.Medium: return R.Messages.difficulty_medium;
            case EDifficulty.Hard: return R.Messages.difficulty_hard;
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

    public static int GetIndex(this EDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EDifficulty.Easy: return 0;
            case EDifficulty.Medium: return 1;
            case EDifficulty.Hard: return 2;
            default:
                throw new UnityException("Unhandled difficulty: " + difficulty);
        }
    }
}
