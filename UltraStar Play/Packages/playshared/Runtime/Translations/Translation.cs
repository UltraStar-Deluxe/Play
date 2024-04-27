using System.Collections.Generic;
using System.Globalization;
using ProTrans;
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
