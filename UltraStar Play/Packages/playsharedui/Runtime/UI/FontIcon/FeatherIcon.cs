using System.Collections.Generic;
using UnityEngine.UIElements;

[UxmlElement]
public partial class FeatherIcon : FontIcon
{
    private static readonly Dictionary<string, string> featherIconNameToCodepointCache = new();

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
