using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LocaleInfoUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        countryCodeEntries = null;
    }

    private static List<Dictionary<string, string>> countryCodeEntries;

    public static string GetTwoLetterCountryCode(string language)
    {
        // handshake_country_language_locale_codes.json from https://gist.github.com/justincoh/80f97efdd21b516e3274973a003a1b08
        Dictionary<string, string> localeInfo = GetLocaleInfo(language);
        if (localeInfo == null)
        {
            return "";
        }

        string twoLetterCountryCode = localeInfo["ISO639-2 Lang"];
        return twoLetterCountryCode;
    }

    private static Dictionary<string, string> GetLocaleInfo(string language)
    {
        if (language.IsNullOrEmpty())
        {
            return null;
        }

        // handshake_country_language_locale_codes.json from https://gist.github.com/justincoh/80f97efdd21b516e3274973a003a1b08
        if (countryCodeEntries.IsNullOrEmpty())
        {
            TextAsset localeInfoTextAsset = Resources.Load<TextAsset>("handshake_country_language_locale_codes.json");
            countryCodeEntries = JsonConverter.FromJson<List<Dictionary<string, string>>>(localeInfoTextAsset.text);
        }
        List<Dictionary<string,string>> matchingEntries = countryCodeEntries.Where(localeInfo =>
            string.Equals(localeInfo["Language"], language, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(localeInfo["ISO639-2 Lang"], language, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
        return matchingEntries.FirstOrDefault();
    }
}
