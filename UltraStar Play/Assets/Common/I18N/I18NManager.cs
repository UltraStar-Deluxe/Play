using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;

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

    private const string I18NFolder = "I18N";
    private const string PropertiesFileExtension = ".properties";
    private const string PropertiesFileName = "messages";

    public static I18NManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<I18NManager>("I18NManager");
        }
    }

    public bool logInfo;

    public bool isOverwriteLanguage;
    public bool updateAllTranslationsWhenReloadingLangauge;
    public SystemLanguage overwriteLanguage;

    [ReadOnly]
    public SystemLanguage language;

    // Fields are static to be persisted across scenes
    private static Dictionary<string, string> currentLanguageMessages;
    private static Dictionary<string, string> fallbackMessages;

    private CoroutineManager coroutineManager;

    private void Awake()
    {
        language = SettingsManager.Instance.Settings.GameSettings.language;
        if (currentLanguageMessages.IsNullOrEmpty()
            || fallbackMessages.IsNullOrEmpty())
        {
            UpdateCurrentLanguageAndTranslations(() => UpdateAllTranslationsInScene());
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }

        if (currentLanguageMessages.IsNullOrEmpty()
                || fallbackMessages.IsNullOrEmpty())
        {
            UpdateCurrentLanguageAndTranslations();
        }
    }
#endif

    public List<string> GetKeys()
    {
        return fallbackMessages.Keys.ToList();
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

    public void UpdateCurrentLanguageAndTranslations(Action callback = null)
    {
        currentLanguageMessages = new Dictionary<string, string>();
        fallbackMessages = new Dictionary<string, string>();
        SystemLanguage selectedLanguage = Application.isEditor && isOverwriteLanguage
            ? overwriteLanguage
            : language;

        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }

        // Flags to execute the callback when all has been loaded.
        bool loadedFallbackTranslationsFinished = false;
        bool loadedTranslationsFinished = false;

        // Load the default properties file
        string fallbackPropertiesFilePath = GetPropertiesFilePath(PropertiesFileName);
        coroutineManager.StartCoroutineAlsoForEditor(WebRequestUtils.LoadTextFromUri(ApplicationUtils.GetStreamingAssetsUri(fallbackPropertiesFilePath),
            (loadedText) =>
            {
                LoadPropertiesFromText(loadedText, fallbackMessages, fallbackPropertiesFilePath);

                loadedFallbackTranslationsFinished = true;
                if (loadedFallbackTranslationsFinished && loadedTranslationsFinished && callback != null)
                {
                    callback();
                }
            }));

        // Load the properties file of the current language
        string propertiesFileNameWithCountryCode = PropertiesFileName + GetCountryCodeSuffixForPropertiesFile(selectedLanguage);
        string propertiesFilePath = GetPropertiesFilePath(propertiesFileNameWithCountryCode);
        coroutineManager.StartCoroutineAlsoForEditor(WebRequestUtils.LoadTextFromUri(ApplicationUtils.GetStreamingAssetsUri(propertiesFilePath),
            (loadedText) =>
            {
                LoadPropertiesFromText(loadedText, currentLanguageMessages, propertiesFilePath);

                loadedTranslationsFinished = true;
                if (loadedFallbackTranslationsFinished && loadedTranslationsFinished && callback != null)
                {
                    callback();
                }
            }));
    }

    public void UpdateAllTranslationsInScene()
    {
        I18NText[] i18nTexts = GameObject.FindObjectsOfType<I18NText>();
        Debug.Log($"Updating I18NText instances in scene: {i18nTexts.Length}");
        foreach (I18NText i18nText in i18nTexts)
        {
            i18nText.UpdateTranslation();
        }
    }

    private void LoadPropertiesFromText(string text, Dictionary<string, string> targetDictionary, string textSource)
    {
        targetDictionary.Clear();
        PropertiesFileParser.ParseText(text).ForEach(entry => targetDictionary.Add(entry.Key, entry.Value));

        if (logInfo)
        {
            Debug.Log("Loaded " + targetDictionary.Count + " translations from " + textSource);
        }
    }

    private string GetCountryCodeSuffixForPropertiesFile(SystemLanguage language)
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
