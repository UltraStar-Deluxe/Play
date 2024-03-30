using System.Collections.Generic;
using System.Globalization;
using ProTrans;
using UnityEngine;

public class ResourcesFolderPropertiesFileProvider : IPropertiesFileProvider
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        TranslationConfig.Singleton.PropertiesFileProvider = new ResourcesFolderPropertiesFileProvider();
    }

    private readonly Dictionary<CultureInfo, PropertiesFile> cultureInfoToPropertiesFile = new();

    public PropertiesFile GetPropertiesFile(CultureInfo cultureInfo)
    {
        CultureInfo cultureInfoOrDefault = cultureInfo ?? TranslationConfig.Singleton.DefaultCultureInfo;
        if (cultureInfoToPropertiesFile.TryGetValue(cultureInfoOrDefault, out PropertiesFile cachedPropertiesFile))
        {
            return cachedPropertiesFile;
        }

        string languageAndRegionSuffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo);
        TextAsset textAsset = Resources.Load<TextAsset>($"Translations/messages{languageAndRegionSuffix}");
        if (textAsset == null)
        {
            return null;
        }

        PropertiesFile propertiesFile = PropertiesFileParser.ParseText(textAsset.text, cultureInfo);
        cultureInfoToPropertiesFile[cultureInfoOrDefault] = propertiesFile;

        return propertiesFile;
    }
}
