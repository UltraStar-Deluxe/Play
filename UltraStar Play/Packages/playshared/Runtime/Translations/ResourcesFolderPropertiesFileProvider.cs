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

    public PropertiesFile GetPropertiesFile(CultureInfo cultureInfo)
    {
        string languageAndRegionSuffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo);
        TextAsset textAsset = Resources.Load<TextAsset>($"Translations/messages{languageAndRegionSuffix}");
        if (textAsset == null)
        {
            return null;
        }
        return PropertiesFileParser.ParseText(textAsset.text, cultureInfo);
    }
}
