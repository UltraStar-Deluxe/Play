using UnityEngine;
using System.Collections;

/// Original code by Martin Schultz (special thanks to him!):
/// https://github.com/MartinSchultz/unity3d/blob/master/LanguageHelper.cs
public static class LanguageHelper
{

    /// <summary>
    /// Helps to convert Unity's SystemLanguage enum to a 
    /// 2 letter ISO country code. There is unfortunately not more
    /// countries available as Unity's enum does not enclose all
    /// countries.
    /// </summary>
    /// <returns>The 2-letter ISO code from system language.</returns>
    public static string Get2LetterIsoCodeFromSystemLanguage(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.Afrikaans: return "AF";
            case SystemLanguage.Arabic: return "AR";
            case SystemLanguage.Basque: return "EU";
            case SystemLanguage.Belarusian: return "BY";
            case SystemLanguage.Bulgarian: return "BG";
            case SystemLanguage.Catalan: return "CA";
            case SystemLanguage.Chinese: return "ZH";
            case SystemLanguage.Czech: return "CS";
            case SystemLanguage.Danish: return "DA";
            case SystemLanguage.Dutch: return "NL";
            case SystemLanguage.English: return "EN";
            case SystemLanguage.Estonian: return "ET";
            case SystemLanguage.Faroese: return "FO";
            case SystemLanguage.Finnish: return "FI";
            case SystemLanguage.French: return "FR";
            case SystemLanguage.German: return "DE";
            case SystemLanguage.Greek: return "EL";
            case SystemLanguage.Hebrew: return "IW";
            case SystemLanguage.Hungarian: return "HU";
            case SystemLanguage.Icelandic: return "IS";
            case SystemLanguage.Indonesian: return "IN";
            case SystemLanguage.Italian: return "IT";
            case SystemLanguage.Japanese: return "JA";
            case SystemLanguage.Korean: return "KO";
            case SystemLanguage.Latvian: return "LV";
            case SystemLanguage.Lithuanian: return "LT";
            case SystemLanguage.Norwegian: return "NO";
            case SystemLanguage.Polish: return "PL";
            case SystemLanguage.Portuguese: return "PT";
            case SystemLanguage.Romanian: return "RO";
            case SystemLanguage.Russian: return "RU";
            case SystemLanguage.SerboCroatian: return "SH";
            case SystemLanguage.Slovak: return "SK";
            case SystemLanguage.Slovenian: return "SL";
            case SystemLanguage.Spanish: return "ES";
            case SystemLanguage.Swedish: return "SV";
            case SystemLanguage.Thai: return "TH";
            case SystemLanguage.Turkish: return "TR";
            case SystemLanguage.Ukrainian: return "UK";
            case SystemLanguage.Unknown: return "EN";
            case SystemLanguage.Vietnamese: return "VI";
            default:
                Debug.LogError("Unkown system language: " + lang + ". Returning English as fallback.");
                return "EN";
        }
    }
}