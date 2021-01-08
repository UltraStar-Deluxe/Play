using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using System.IO;
using UnityEngine.SceneManagement;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[ExecuteInEditMode]
public class I18NManager : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        currentLanguageMessages?.Clear();
        fallbackMessages?.Clear();
    }

    public const string I18NFolder = "I18N";
    public const string PropertiesFileExtension = ".properties";
    public const string PropertiesFileName = "messages";

    public static I18NManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<I18NManager>("I18NManager");
        }
    }

    public bool logInfo;

    public bool isOverwriteLanguage;
    public SystemLanguage overwriteLanguage;

    // Fields are static to be persisted across scenes
    private static Dictionary<string, string> currentLanguageMessages;
    private static Dictionary<string, string> fallbackMessages;

    private void Awake()
    {
        if (fallbackMessages.IsNullOrEmpty())
        {
            UpdateCurrentLanguageAndTranslations();
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (fallbackMessages.IsNullOrEmpty())
        {
            UpdateCurrentLanguageAndTranslations();
        }
    }
#endif

    public List<string> GetKeys()
    {
        return fallbackMessages.Keys.ToList();
    }

    public static string GetTranslation(string key, params string[] placeholderStrings)
    {
        if (placeholderStrings.Length % 2 != 0)
        {
            Debug.LogWarning($"Uneven number of placeholders for '{key}'. Format in array should be [key1, value1, key2, value2, ...]");
        }

        // Create dictionary from placeholderStrings
        Dictionary<string, string> placeholders = new Dictionary<string, string>();
        for (int i = 0; i < placeholderStrings.Length && i + 1 < placeholderStrings.Length; i += 2)
        {
            string placeholderKey = placeholderStrings[i];
            string placeholderValue = placeholderStrings[i + 1];
            placeholders[placeholderKey] = placeholderValue;
        }
        return GetTranslation(key, placeholders);
    }

    public static string GetTranslation(string key, Dictionary<string, string> placeholders)
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

    public static string GetTranslation(string key)
    {
        if (currentLanguageMessages.TryGetValue(key, out string translation))
        {
            return translation;
        }
        else
        {
            Debug.LogWarning($"Missing translation in language '{SettingsManager.Instance.Settings.GameSettings.language}' for key '{key}'");
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

    public void UpdateCurrentLanguageAndTranslations()
    {
        currentLanguageMessages = new Dictionary<string, string>();
        fallbackMessages = new Dictionary<string, string>();
        SystemLanguage selectedLanguage = Application.isEditor && isOverwriteLanguage
            ? overwriteLanguage
            : SettingsManager.Instance.Settings.GameSettings.language;

        // Load the default properties file
        string fallbackPropertiesFilePath = GetPropertiesFilePath(PropertiesFileName);
        string fallbackPropertiesFileContent = File.ReadAllText(fallbackPropertiesFilePath);
        LoadPropertiesFromText(fallbackPropertiesFileContent, fallbackMessages);

        // Load the properties file of the current language
        string propertiesFileNameWithCountryCode = PropertiesFileName + GetCountryCodeSuffixForPropertiesFile(selectedLanguage);
        string propertiesFilePath = GetPropertiesFilePath(propertiesFileNameWithCountryCode);
        string propertiesFileContent = File.ReadAllText(propertiesFilePath);
        LoadPropertiesFromText(propertiesFileContent, currentLanguageMessages);

        if (logInfo)
        {
            Debug.Log("Loaded " + fallbackMessages.Count + " translations from " + fallbackPropertiesFilePath);
            Debug.Log("Loaded " + currentLanguageMessages.Count + " translations from " + propertiesFilePath);
        }

        UpdateAllTranslationsInScene();
    }

    private static void UpdateAllTranslationsInScene()
    {
        LinkedList<ITranslator> translators = new LinkedList<ITranslator>();
        Scene scene = SceneManager.GetActiveScene();
        if (scene != null && scene.isLoaded)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                UpdateTranslationsRecursively(rootObject, translators);
            }
            Debug.Log($"Updated ITranslator instances in scene: {translators.Count}");
        }
    }

    private static void UpdateTranslationsRecursively(GameObject gameObject, LinkedList<ITranslator> translators)
    {
        MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // The script can be null if it is a missing component.
            if (script == null)
            {
                continue;
            }

            if (script is ITranslator translator)
            {
                translators.AddLast(translator);
                translator.UpdateTranslation();
            }
        }

        foreach (Transform child in gameObject.transform)
        {
            UpdateTranslationsRecursively(child.gameObject, translators);
        }
    }

    private static void LoadPropertiesFromText(string text, Dictionary<string, string> targetDictionary)
    {
        targetDictionary.Clear();
        PropertiesFileParser.ParseText(text).ForEach(entry => targetDictionary.Add(entry.Key, entry.Value));
    }

    private static string GetCountryCodeSuffixForPropertiesFile(SystemLanguage language)
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

    private static string GetPropertiesFilePath(string propertiesFileNameWithCountryCode)
    {
        string path = I18NFolder + "/" + propertiesFileNameWithCountryCode + PropertiesFileExtension;
        path = ApplicationUtils.GetStreamingAssetsPath(path);
        return path;
    }
}
