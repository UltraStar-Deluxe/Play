using System.Collections.Generic;
using System.Globalization;
using ProTrans;
using UnityEngine;

public static class Translation
{
    public const string TranslationKeyPrefix = "$";

    public static TranslationResult Get(string key, params object[] placeholderStrings)
    {
        return TranslationResult.Of(ProTrans.Translation.Get(key, placeholderStrings));
    }

    public static TranslationResult Get(string key, Dictionary<string, string> placeholders)
    {
        return TranslationResult.Of(ProTrans.Translation.Get(key, placeholders));
    }

    public static bool TryGet(string key, Dictionary<string, string> placeholders, out TranslationResult translationResult)
    {
        bool result = ProTrans.Translation.TryGet(key, placeholders, out string translation);
        translationResult = TranslationResult.Of(translation);
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
