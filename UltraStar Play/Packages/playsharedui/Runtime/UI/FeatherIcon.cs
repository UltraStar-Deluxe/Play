using System.Collections.Generic;
using UnityEngine.UIElements;

public class FeatherIcon : FontIcon
{
    private static readonly Dictionary<string, string> featherIconNameToCodepointCache = new();

    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<FeatherIcon, UxmlTraits> {}

    public new class UxmlTraits : FontIcon.UxmlTraits {}

    protected override bool TryGetCodepointByIconName(string iconName, out string codepoint)
    {
        return TryGetCodepointByIconNameFromTextAsset(
            iconName,
            "FeatherIcons",
            featherIconNameToCodepointCache,
            out codepoint);
    }

    protected override string GetIconFontUssClass()
    {
        return "featherIcon";
    }
}
