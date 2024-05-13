using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using ProTrans;
using UnityEditor;
using UnityEngine;

public static class DeeplTranslationMenuItems
{
    private const string AuthKeyEnvironmentVariable = "DEEPL_AUTH_KEY";

    private static readonly string targetFolder = $"{Application.dataPath}/../Packages/playshared/Runtime/Resources/Translations";

    private static readonly bool debugRun = false;

    private static readonly List<CultureInfo> targetLanguages = new List<CultureInfo>()
    {
        // Ordered by total number of speakers ( https://en.wikipedia.org/wiki/List_of_languages_by_total_number_of_speakers )
        // 1. English, skipped because default
        // 2. Chinese (Simplified)
        new CultureInfo("zh"),
        // 3. Hindi (India), skipped because not supported by DeepL
        // 4. Spanish (Spain)
        new CultureInfo("es"),
        // 5. French (France)
        new CultureInfo("fr"),
        // 6. Arabic, skipped because Right to Left text not supported by Unity ( https://forum.unity.com/threads/right-to-left-and-arabic-support-for-labels.1311900/ )
        // 7. Bengali (Bangladesh), skipped because not supported by DeepL
        // 8. Portuguese (Portugal)
        new CultureInfo("pt"),
        // 9. Russian (Russia)
        new CultureInfo("ru"),

        // 12. German (Germany)
        new CultureInfo("de"),
        // 13. Japanese (Japan)
        new CultureInfo("ja"),

        // 24. Korean (Korea), because has karaoke culture
        new CultureInfo("ko"),

        // 29. Italian (Italy), because had an UltraStar community
        new CultureInfo("it"),
        // Polish (Poland), because of user contribution on GitHub
        new CultureInfo("pl"),
    };

    /**
     * Languages where DeepL does not support "formality" parameter.
     */
    public static readonly List<string> languagesWithoutFormality = new List<string>() { "zh", "ar", "ko" };

    /**
     * List of RegEx patterns for translation keys that should not be translated.
     */
    private static readonly List<string> ignoredTranslationKeyPatterns = new List<string>()
    {
        // Links to websites are the same for all languages. The website should provide an option to switch language.
        "uri_.*",
        "companionApp_title",
    };

    [MenuItem("Tools/DeepL/Translate properties files")]
    public static async void CreateTranslationConstants()
    {
        Translation.InitTranslationConfig();
        LogWarningAboutNonListedTargetLanguages();

        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        string authKey = Environment.GetEnvironmentVariable(AuthKeyEnvironmentVariable);

        List<ProTransTranslation> proTransTranslations = defaultPropertiesFile.Dictionary
            .Select(entry => new ProTransTranslation
            {
                key = entry.Key,
                value = entry.Value,
            })
            .ToList();
        if (debugRun)
        {
            Debug.Log("Taking only first few translations because this is a debug run");
            proTransTranslations = proTransTranslations.Take(10).ToList();
        }

        foreach (CultureInfo targetLanguage in targetLanguages)
        {
            Dictionary<string, string> existingTranslations = GetExistingTranslations(targetLanguage);
            List<ProTransTranslation> missingProTransTranslations = proTransTranslations
                .Where(proTransTranslation => !existingTranslations.ContainsKey(proTransTranslation.key)
                                              && !IsIgnoredTranslationKey(proTransTranslation.key))
                .ToList();
            if (missingProTransTranslations.IsNullOrEmpty())
            {
                Debug.Log($"No missing translations for '{targetLanguage}'");
                continue;
            }

            Dictionary<string, string> translationResults = await TranslateViaDeepL(
                authKey,
                targetLanguage.ToString(),
                missingProTransTranslations);

            // Merge result with already existing translations
            Dictionary<string, string> updatedDictionary = new(existingTranslations);
            translationResults.ForEach(entry => updatedDictionary[entry.Key] = entry.Value);

            // Write to file
            WritePropertiesFile(new PropertiesFile(updatedDictionary, targetLanguage));

            if (debugRun)
            {
                Debug.Log("Skipping other languages because this is a debug run");
                return;
            }
        }
    }

    private static Dictionary<string,string> GetExistingTranslations(CultureInfo targetLanguage)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        PropertiesFile propertiesFile = Translation.GetPropertiesFile(targetLanguage);
        if (propertiesFile != null)
        {
            propertiesFile.Dictionary.ForEach(entry => result.Add(entry.Key, entry.Value));
        }
        return result;
    }

    private static bool IsIgnoredTranslationKey(string translationKey)
    {
        return ignoredTranslationKeyPatterns.AnyMatch(pattern => Regex.IsMatch(translationKey, pattern));
    }

    private static void LogWarningAboutNonListedTargetLanguages()
    {
        List<CultureInfo> nonDefaultCultureInfos = Translation.GetTranslatedCultureInfos()
            .Except(new List<CultureInfo>() { new CultureInfo("en") })
            .ToList();
        List<CultureInfo> nonListedLanguageCodes = nonDefaultCultureInfos
            .Where(presentLanguage => targetLanguages.AllMatch(listedLanguage => !Equals(presentLanguage, listedLanguage)))
            .ToList();
        if (!nonDefaultCultureInfos.IsNullOrEmpty())
        {
            Debug.LogWarning($"The following languages will not be translated. " +
                             $"Add them to the target translate list to translate them: {nonListedLanguageCodes.JoinWith(", ")}");
        }
    }

    private static async Task<Dictionary<string, string>> TranslateViaDeepL(
        string authKey,
        string targetLanguage,
        List<ProTransTranslation> proTransTranslations)
    {
        string[] texts = proTransTranslations
            .Select(it => it.value
                // Escape placeholders
                .Replace("{", "<x>")
                .Replace("}", "</x>"))
            .ToArray();
        Debug.Log($"Translating {texts.Length} texts to '{targetLanguage}' via DeepL:\n    {texts.JoinWith("\n    ")}");

        DeeplResponse response = await PerformDeeplRequest(
            authKey,
            texts,
            targetLanguage);

        Debug.Log($"DeepL response: {response.translations.Count} translations:\n    " +
                  $"{response.translations.Select(it => it.text).JoinWith("\n    ")}");

        // Merge with translation translationKey
        Dictionary<string, string> keyToTranslatedValue = new();
        for (int i = 0; i < response.translations.Count; i++)
        {
            ProTransTranslation proTransTranslation = proTransTranslations[i];
            DeeplTranslation deeplTranslation = response.translations[i];

            keyToTranslatedValue[proTransTranslation.key] = deeplTranslation.text
                // Unescape placeholders
                .Replace("<x>", "{")
                .Replace("</x>", "}")
                // Escape whitespace
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        return keyToTranslatedValue;
    }

    private static void WritePropertiesFile(PropertiesFile propertiesFile)
    {
        string filePath = $"{targetFolder}/messages_{propertiesFile.CultureInfo}.properties";
        Debug.Log($"Writing file {filePath}");

        DirectoryUtils.CreateDirectory(Path.GetDirectoryName(filePath));
        List<string> lines = propertiesFile.Dictionary
            .OrderBy(entry => entry.Key, StringComparer.InvariantCultureIgnoreCase)
            .Select(entry => $"{entry.Key}={EscapeTranslationValue(entry.Value)}")
            .ToList();
        File.WriteAllLines(filePath, lines);
    }

    private static string EscapeTranslationValue(string value)
    {
        return value
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\t", "\\t");
    }

    private class ProTransTranslation
    {
        public string key;
        public string value;
    }

    private static async Task<DeeplResponse> PerformDeeplRequest(
        string authKey,
        string[] strings,
        string targetLanguage)
    {
        object body;
        if (languagesWithoutFormality.Contains(targetLanguage))
        {
            body = new
            {
                text = strings,
                source_lang = "EN",
                target_lang = targetLanguage,
                tag_handling = "xml",
                ignore_tags = new[] { "x" },
                context = "karaoke game with song editor",
            };
        }
        else
        {
            body = new
            {
                text = strings,
                source_lang = "EN",
                target_lang = targetLanguage,
                formality = "less",
                tag_handling = "xml",
                ignore_tags = new[] { "x" },
                context = "karaoke game with song editor",
            };
        }
        string jsonBody = JsonConverter.ToJson(body);
        ClipboardUtils.CopyToClipboard(jsonBody);
        Debug.Log($"Copied JSON body to clipboard:\n{jsonBody}");

        return await "https://api-free.deepl.com/v2/translate"
            .WithHeader("Authorization", $"DeepL-Auth-Key {authKey}")
            .WithHeader("User-Agent", "MyApp/1.2.3")
            .WithHeader("Content-Type", "application/json")
            .PostJsonAsync(body)
            .ReceiveJson<DeeplResponse>();
    }

    private class DeeplResponse
    {
        public List<DeeplTranslation> translations;
    }

    private class DeeplTranslation
    {
        public string detected_source_language;
        public string text;
    }
}
