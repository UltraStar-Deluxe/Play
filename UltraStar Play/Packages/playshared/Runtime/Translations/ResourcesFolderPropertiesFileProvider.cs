using System.Globalization;
using ProTrans;
using UnityEngine;

public class ResourcesFolderPropertiesFileProvider : IPropertiesFileProvider
{
    public PropertiesFile GetPropertiesFile(CultureInfo cultureInfo)
    {
        string languageAndRegionSuffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo);
        TextAsset textAsset = Resources.Load<TextAsset>($"Translations/messages{languageAndRegionSuffix}");
        if (textAsset == null)
        {
            return null;
        }

        PropertiesFile propertiesFile = PropertiesFileParser.ParseText(textAsset.text, cultureInfo);

        return propertiesFile;
    }
}
