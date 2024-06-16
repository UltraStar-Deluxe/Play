using System.Globalization;
using ProTrans;
using UnityEngine.UIElements;

public class LanguageChooserControl : DropdownFieldControl<CultureInfo>
{
    public LanguageChooserControl(DropdownField dropdownField)
        : base(dropdownField,
            Translation.GetTranslatedCultureInfos(),
                TranslationConfig.Singleton.CurrentCultureInfo,
                GetCultureInfoDisplayString)
    {
    }

    private static string GetCultureInfoDisplayString(CultureInfo cultureInfo)
    {
        string suffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo).ToLowerInvariant();
        // Always use English name of language, i.e., use default CultureInfo to get language name.
        Translation translation = Translation.Get($"language{suffix}", TranslationConfig.Singleton.DefaultCultureInfo);
        return translation;
    }
}
