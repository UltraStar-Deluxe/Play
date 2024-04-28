using System;
using System.Collections.Generic;
using System.Globalization;
using ProTrans;
using UnityEditor.Graphs;
using UnityEngine;

public readonly struct Translation
{
    public const string TranslationKeyPrefix = "$";

    public static Translation Empty { get; } = Of("");

    private readonly string value;
    public string Value => value ?? "";

    private Translation(string value)
    {
        this.value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(Translation it) => it.Value;

    public static Translation Of(string value)
    {
        return new Translation(value);
    }

    public static Translation Get<T>(T value) where T : Enum
    {
        string typeName = typeof(T).Name;
        string valueName = value.ToString();

        // For example, ENoteDisplayMode.SentenceBySentence has a translation key enum_noteDisplayMode_sentenceBySentence
        if (typeName.StartsWith("E")
            && TryGet($"enum_{typeName.TrimStart('E')}_{valueName}", new Dictionary<string, string>(), out Translation translationResult1))
        {
            return translationResult1;
        }

        if (TryGet($"enum_{typeName}_{valueName}", new Dictionary<string, string>(), out Translation translationResult2))
        {
            return translationResult2;
        }

        Debug.LogWarning($"Missing translation for enum {typeName}.{valueName}, e.g., enum_{typeName.TrimStart('E')}_{valueName}={StringUtils.ToTitleCase(valueName)}");
        return Translation.Of(StringUtils.ToTitleCase(valueName));
    }

    public static Translation Get(string key, params object[] placeholderStrings)
    {
        return Of(ProTrans.Translation.Get(key, placeholderStrings));
    }

    public static Translation Get(string key, Dictionary<string, string> placeholders)
    {
        return Of(ProTrans.Translation.Get(key, placeholders));
    }

    public static bool TryGet(string key, Dictionary<string, string> placeholders, out Translation translationResult)
    {
        bool result = ProTrans.Translation.TryGet(key, placeholders, out string translation);
        translationResult = Of(translation);
        return result;
    }

    public static List<CultureInfo> GetTranslatedCultureInfos()
    {
        return ProTrans.Translation.GetTranslatedCultureInfos();
    }

    public static PropertiesFile GetPropertiesFile(CultureInfo cultureInfo)
    {
        return ProTrans.Translation.GetPropertiesFile(cultureInfo);
    }

    public static CultureInfo GetFallbackCultureInfo(CultureInfo cultureInfo = null)
    {
        return ProTrans.Translation.GetFallbackCultureInfo(cultureInfo);
    }

    public static void InitTranslationConfig()
    {
        TranslationConfig.Singleton.PropertiesFileProvider = new CachingPropertiesFileProvider(new ResourcesFolderPropertiesFileProvider());
        TranslationConfig.Singleton.MissingPlaceholderStrategy = Application.isEditor ? MissingPlaceholderStrategy.Throw : MissingPlaceholderStrategy.Log;
        TranslationConfig.Singleton.UnexpectedPlaceholderStrategy = Application.isEditor ? UnexpectedPlaceholderStrategy.Throw : UnexpectedPlaceholderStrategy.Log;
    }
}
