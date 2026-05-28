using System.Globalization;
using UnityEngine;

public static class ForcedAlignmentConfigurationUtils
{
    public static string GetModelPath(Settings settings)
    {
        string lyricsLanguage = settings.SongEditorSettings.LyricsLanguage;
        
        string twoLetterLanguageCode = NormalizeTwoLetterLanguageCode(lyricsLanguage);
        string languageModelPath =
            ApplicationUtils.GetStreamingAssetsPath(
                $"AiModels/NemoForcedAligner/stt_{twoLetterLanguageCode}_conformer_ctc_large.onnx");
        string configuredModelPath = settings.SongEditorSettings.ForcedAlignmentModelPath;

        string modelPath;
        if (!configuredModelPath.IsNullOrEmpty())
        {
            modelPath = configuredModelPath;
        }
        else if (FileUtils.Exists(languageModelPath))
        {
            modelPath = languageModelPath;
        }
        else
        {
            // No language-specific model found and no custom path configured: fall back to English and warn.
            string fallbackModelPath =
                ApplicationUtils.GetStreamingAssetsPath("AiModels/NemoForcedAligner/stt_en_conformer_ctc_large.onnx");

            // Only warn when the language is not already 'en' (in that case the fallback is the same model)
            // and only once per unique language code to avoid spamming notifications on every alignment run.
            if (twoLetterLanguageCode != "en")
            {
                Log.Warning(() =>
                    $"No suited NeMo Forced Aligner model found, using 'en' model as fallback. language '{lyricsLanguage}', twoLetterLanguageCode: '{twoLetterLanguageCode}', expected model path: '{languageModelPath}'");
                NotificationManager.CreateNotification(Translation.Get("job_forcedAlignment_warning_noModelForLanguage",
                    "language", lyricsLanguage));
            }

            modelPath = fallbackModelPath;
        }

        return modelPath;
    }

    private static string NormalizeTwoLetterLanguageCode(string lyricsLanguage)
    {
        if (lyricsLanguage.IsNullOrEmpty())
        {
            return "en";
        }

        if (lyricsLanguage.Length == 2)
        {
            // Assume this is a two letter language code.
            return lyricsLanguage.ToLowerInvariant();
        }

        try
        {
            CultureInfo cultureInfo = new CultureInfo(lyricsLanguage);
            return cultureInfo.TwoLetterISOLanguageName.ToLowerInvariant();
        }
        catch (CultureNotFoundException)
        {
            Debug.LogError($"Failed to determine two letter language code, using 'en' as fallback. language: '{lyricsLanguage}'");
            return "en";
        }
    }
}
