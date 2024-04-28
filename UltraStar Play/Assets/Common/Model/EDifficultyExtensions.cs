using UnityEngine;

public static class EDifficultyExtensions
{
    public static float GetRoundingDistanceInMidiNotes(this EDifficulty difficulty)
    {
        switch (difficulty)
        {
            case EDifficulty.Easy: return 2;
            case EDifficulty.Medium: return 1;
            case EDifficulty.Hard: return 0.5f;
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
