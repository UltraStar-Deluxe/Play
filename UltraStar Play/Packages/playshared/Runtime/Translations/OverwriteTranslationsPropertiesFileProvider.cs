using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProTrans;

public class OverwriteTranslationsPropertiesFileProvider : IPropertiesFileProvider
{
    private readonly IPropertiesFileProvider basePropertiesFileProvider;

    public OverwriteTranslationsPropertiesFileProvider(IPropertiesFileProvider basePropertiesFileProvider)
    {
        this.basePropertiesFileProvider = basePropertiesFileProvider;
    }

    public PropertiesFile GetPropertiesFile(CultureInfo cultureInfo)
    {
        PropertiesFile basePropertiesFile = basePropertiesFileProvider.GetPropertiesFile(cultureInfo);

        string languageAndRegionSuffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo);
        string absoluteFilePath = ApplicationUtils.GetPersistentDataPath($"Translations/messages{languageAndRegionSuffix}.properties");
        if (!FileUtils.Exists(absoluteFilePath))
        {
            return basePropertiesFile;
        }

        // Overwrite base with custom properties
        PropertiesFile propertiesFile = PropertiesFileParser.ParseText(File.ReadAllText(absoluteFilePath), cultureInfo);
        Dictionary<string, string> mergedDictionary = basePropertiesFile.Dictionary
            .ToDictionary(
                entry => entry.Key,
                entry => propertiesFile.GetValue(entry.Key, entry.Value)
            );
        return new PropertiesFile(mergedDictionary, cultureInfo);
    }
}
