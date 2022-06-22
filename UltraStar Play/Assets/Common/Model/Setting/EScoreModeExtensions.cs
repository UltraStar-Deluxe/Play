using System;
using ProTrans;

public static class ScoreModeExtensions
{
    public static string GetTranslation(this EScoreMode scoreMode)
    {
        switch (scoreMode)
        {
            case EScoreMode.Individual:
                return TranslationManager.GetTranslation(R.Messages.enum_scoreMode_individual);
            case EScoreMode.CommonAverage:
                return TranslationManager.GetTranslation(R.Messages.enum_scoreMode_commonAverage);
            case EScoreMode.None:
                return TranslationManager.GetTranslation(R.Messages.enum_scoreMode_none);
            default:
                throw new ArgumentOutOfRangeException(nameof(scoreMode), scoreMode, null);
        }
    }
}
