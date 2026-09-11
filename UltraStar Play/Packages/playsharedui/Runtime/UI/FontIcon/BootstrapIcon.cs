using System.Collections.Generic;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BootstrapIcon : FontIcon
{
    private static readonly Dictionary<string, string> bootstrapIconNameToCodepointCache = new();

    protected override bool TryGetCodepointByIconName(string iconName, out string codepoint)
    {
        if (TryGetCodepointByIconNameFromTextAsset(
            iconName,
            "BootstrapIcons",
            bootstrapIconNameToCodepointCache,
            out string codepointDecimal))
        {
            string codepointHex = int.Parse(codepointDecimal).ToString("X");
            codepoint = codepointHex;
            return true;
        }

        codepoint = "";
        return false;
    }

    protected override string GetIconFontUssClass()
    {
        return "bootstrapIcon";
    }
}
