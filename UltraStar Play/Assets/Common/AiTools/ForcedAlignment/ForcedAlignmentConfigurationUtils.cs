using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class ForcedAlignmentConfigurationUtils
{
    private static readonly Dictionary<ELyricsLanguage, string> languageToTwoLetterCode =
        new Dictionary<ELyricsLanguage, string>
        {
            { ELyricsLanguage.Afrikaans, "af" },
            { ELyricsLanguage.Arabic, "ar" },
            { ELyricsLanguage.Armenian, "hy" },
            { ELyricsLanguage.Azerbaijani, "az" },
            { ELyricsLanguage.Belarusian, "be" },
            { ELyricsLanguage.Bosnian, "bs" },
            { ELyricsLanguage.Bulgarian, "bg" },
            { ELyricsLanguage.Catalan, "ca" },
            { ELyricsLanguage.Chinese, "zh" },
            { ELyricsLanguage.Croatian, "hr" },
            { ELyricsLanguage.Czech, "cs" },
            { ELyricsLanguage.Danish, "da" },
            { ELyricsLanguage.Dutch, "nl" },
            { ELyricsLanguage.English, "en" },
            { ELyricsLanguage.Estonian, "et" },
            { ELyricsLanguage.Finnish, "fi" },
            { ELyricsLanguage.French, "fr" },
            { ELyricsLanguage.Galician, "gl" },
            { ELyricsLanguage.German, "de" },
            { ELyricsLanguage.Greek, "el" },
            { ELyricsLanguage.Hebrew, "he" },
            { ELyricsLanguage.Hindi, "hi" },
            { ELyricsLanguage.Hungarian, "hu" },
            { ELyricsLanguage.Icelandic, "is" },
            { ELyricsLanguage.Indonesian, "id" },
            { ELyricsLanguage.Italian, "it" },
            { ELyricsLanguage.Japanese, "ja" },
            { ELyricsLanguage.Kannada, "kn" },
            { ELyricsLanguage.Kazakh, "kk" },
            { ELyricsLanguage.Korean, "ko" },
            { ELyricsLanguage.Latvian, "lv" },
            { ELyricsLanguage.Lithuanian, "lt" },
            { ELyricsLanguage.Macedonian, "mk" },
            { ELyricsLanguage.Malay, "ms" },
            { ELyricsLanguage.Marathi, "mr" },
            { ELyricsLanguage.Maori, "mi" },
            { ELyricsLanguage.Nepali, "ne" },
            { ELyricsLanguage.Norwegian, "no" },
            { ELyricsLanguage.Persian, "fa" },
            { ELyricsLanguage.Polish, "pl" },
            { ELyricsLanguage.Portuguese, "pt" },
            { ELyricsLanguage.Romanian, "ro" },
            { ELyricsLanguage.Russian, "ru" },
            { ELyricsLanguage.Serbian, "sr" },
            { ELyricsLanguage.Slovak, "sk" },
            { ELyricsLanguage.Slovenian, "sl" },
            { ELyricsLanguage.Spanish, "es" },
            { ELyricsLanguage.Swahili, "sw" },
            { ELyricsLanguage.Swedish, "sv" },
            { ELyricsLanguage.Tagalog, "tl" },
            { ELyricsLanguage.Tamil, "ta" },
            { ELyricsLanguage.Thai, "th" },
            { ELyricsLanguage.Turkish, "tr" },
            { ELyricsLanguage.Ukrainian, "uk" },
            { ELyricsLanguage.Urdu, "ur" },
            { ELyricsLanguage.Vietnamese, "vi" },
            { ELyricsLanguage.Welsh, "cy" },
        };

    public static string GetModelPath(Settings settings)
    {
        string lyricsLanguage = settings.SongEditorSettings.LyricsLanguage;

        string twoLetterLanguageCode = GetTwoLetterLanguageCode(lyricsLanguage);
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

    private static string GetTwoLetterLanguageCode(string lyricsLanguage)
    {
        if (lyricsLanguage.IsNullOrEmpty())
        {
            return "en";
        }

        if (lyricsLanguage.Length == 2)
        {
            // Assume this is already a two-letter language code.
            return lyricsLanguage.ToLowerInvariant();
        }
        
        if (TryGetTwoLetterLanguageCodeViaCultureInfo(lyricsLanguage, out string result))
        {
            return result.ToLowerInvariant();
        }
        
        // Some custom languages are not handled by CultureInfo, e.g., "Persian". Try to use the explicit mapping.
        if (Enum.TryParse(lyricsLanguage, true, out ELyricsLanguage language)
            && languageToTwoLetterCode.TryGetValue(language, out string code))
        {
            return code.ToLowerInvariant();
        }

        // Fall back to "en"
        string errorMessage = $"Failed to determine two letter language code, using 'en' as fallback. language: '{lyricsLanguage}'";
        Debug.LogError(errorMessage);
        NotificationManager.CreateNotification(Translation.Of(errorMessage));

        return "en";
    }

    private static bool TryGetTwoLetterLanguageCodeViaCultureInfo(string lyricsLanguage, out string result)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo(lyricsLanguage);
            result = cultureInfo.TwoLetterISOLanguageName.ToLowerInvariant();
            return true;
        }
        catch (CultureNotFoundException)
        {
            result = "";
            return false;
        }
    }
}
