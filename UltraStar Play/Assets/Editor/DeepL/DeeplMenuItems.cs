using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using ProTrans;
using UnityEditor;
using UnityEngine;

public static class DeeplTranslationMenuItems
{
    private const string AuthKeyEnvironmentVariable = "DEEPL_AUTH_KEY";

    private static readonly string targetFolder = $"{Application.dataPath}/../Packages/playshared/Runtime/Resources/Translations";

    [MenuItem("Tools/DeepL/Translate properties files")]
    public static async void CreateTranslationConstants()
    {
        Translation.InitTranslationConfig();

        PropertiesFile defaultPropertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        // List<CultureInfo> nonDefaultCultureInfos = Translation.GetTranslatedCultureInfos()
        //     .Except(new List<CultureInfo>() { new CultureInfo("en") })
        //     .ToList();

        // TODO: Remove to translate all
        List<CultureInfo> nonDefaultCultureInfos = new List<CultureInfo>() { new CultureInfo("de"), new CultureInfo("fr") };

        string authKey = Environment.GetEnvironmentVariable(AuthKeyEnvironmentVariable);

        List<ProTransTranslation> proTransTranslations = defaultPropertiesFile.Dictionary
            // TODO: remove to translate all
            .Take(10)
            .Select(entry => new ProTransTranslation
            {
                key = entry.Key,
                value = entry.Value,
            })
            .ToList();

        foreach (CultureInfo cultureInfo in nonDefaultCultureInfos)
        {
            PropertiesFile propertiesFile = Translation.GetPropertiesFile(cultureInfo);
            List<ProTransTranslation> missingProTransTranslations = proTransTranslations
                .Where(proTransTranslation => !propertiesFile.Dictionary.ContainsKey(proTransTranslation.key))
                .ToList();

            if (missingProTransTranslations.IsNullOrEmpty())
            {
                Debug.Log($"No missing translations for '{cultureInfo}'");
                continue;
            }

            Dictionary<string, string> translatedMissingValues = await TranslateViaDeepL(
                authKey,
                cultureInfo.ToString(),
                missingProTransTranslations);

            Dictionary<string, string> updatedDictionary = new(propertiesFile.Dictionary);
            translatedMissingValues.ForEach(entry => updatedDictionary[entry.Key] = entry.Value);

            PropertiesFile updatedPropertiesFile = new(updatedDictionary, cultureInfo);

            WritePropertiesFile(updatedPropertiesFile);
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

        // Merge with translation key
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
            .Select(entry => $"{entry.Key}={entry.Value}")
            .OrderBy(line => line, StringComparer.InvariantCultureIgnoreCase)
            .ToList();
        File.WriteAllLines(filePath, lines);
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
        return await "https://api-free.deepl.com/v2/translate"
            .WithHeader("Authorization", $"DeepL-Auth-Key {authKey}")
            .WithHeader("User-Agent", "MyApp/1.2.3")
            .WithHeader("Content-Type", "application/json")
            .PostJsonAsync(new
            {
                text = strings,
                source_lang = "EN",
                target_lang = targetLanguage,
                formality = "less",
                tag_handling = "xml",
                ignore_tags = new[] { "x" },
                context = "karaoke game with song editor",
            })
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
