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
        return Translation.Get($"language{suffix}");
    }
}
