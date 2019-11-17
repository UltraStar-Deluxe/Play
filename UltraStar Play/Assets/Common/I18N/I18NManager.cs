using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class I18NManager : MonoBehaviour
{
    private const string I18NFolder = "I18NMessages";
    private const string PropertiesFileExtension = ".properties";
    private const string PropertiesFileName = "messages";

    public static I18NManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<I18NManager>("I18NManager");
        }
    }

    public bool isOverwriteSystemLanguage;
    public SystemLanguage overwriteLanguage;

    private SystemLanguage language;

    private Dictionary<string, string> currentLanguageMessages = new Dictionary<string, string>();
    private Dictionary<string, string> fallbackMessages = new Dictionary<string, string>();

    public List<string> GetKeys()
    {
        UpdateCurrentLanguageAndTranslations();

        List<string> result = new List<string>();
        foreach (string key in fallbackMessages.Keys)
        {
            result.Add(key);
        }
        return result;
    }

    public string GetTranslation(string key, Dictionary<string, string> placeholders)
    {
        string translation = GetTranslation(key);
        foreach (KeyValuePair<string, string> placeholder in placeholders)
        {
            string placeholderText = "{" + placeholder.Key + "}";
            if (translation.Contains(placeholderText))
            {
                translation = translation.Replace(placeholderText, placeholder.Value);
            }
            else
            {
                Debug.LogWarning($"Translation is missing placeholder {placeholderText}");
            }
        }
        return translation;
    }

    public string GetTranslation(string key)
    {
        UpdateCurrentLanguageAndTranslations();

        if (currentLanguageMessages.TryGetValue(key, out string translation))
        {
            return translation;
        }
        else
        {
            Debug.LogWarning($"Missing translation in language '{language}' for key '{key}'");
            if (fallbackMessages.TryGetValue(key, out string fallbackTranslation))
            {
                return fallbackTranslation;
            }
            else
            {
                Debug.LogError($"No translation for key '{key}'");
                return key;
            }
        }
    }

    private void LoadProperties()
    {
        // Load the default properties file
        string path = GetPropertiesFilePath(PropertiesFileName);
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }
        fallbackMessages = PropertiesFileParser.ParseFile(path);

        // Load the properties file of the current language
        string propertiesFileNameWithCountryCode = PropertiesFileName + GetCountryCodeSuffixForPropertiesFile(language);
        path = GetPropertiesFilePath(propertiesFileNameWithCountryCode);
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }
        currentLanguageMessages = PropertiesFileParser.ParseFile(path);
    }

    /// Decide which language to use.
    /// Update the translation mappings if the language changed or it is not loaded yet.
    private void UpdateCurrentLanguageAndTranslations()
    {
        SystemLanguage oldLangauge = language;

        language = Application.systemLanguage;
        if (Application.isEditor && isOverwriteSystemLanguage)
        {
            language = overwriteLanguage;
        }

        if (oldLangauge != language || fallbackMessages.Count == 0)
        {
            LoadProperties();
        }
    }

    private string GetCountryCodeSuffixForPropertiesFile(SystemLanguage langauge)
    {
        if (language != SystemLanguage.English)
        {
            return "_" + LanguageHelper.Get2LetterIsoCodeFromSystemLanguage(language).ToLower();
        }
        else
        {
            return "";
        }
    }

    private string GetPropertiesFilePath(string propertiesFileNameWithCountryCode)
    {
        string path = I18NFolder + "/" + propertiesFileNameWithCountryCode + PropertiesFileExtension;
        return path;
    }
}
