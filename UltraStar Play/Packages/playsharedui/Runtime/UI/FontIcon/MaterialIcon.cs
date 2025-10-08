using System.Collections.Generic;
using UnityEngine.UIElements;

[UxmlElement]
public partial class MaterialIcon : FontIcon
{
    private static readonly Dictionary<string, string> materialDesignIconNameToCodepointCache = new();

    protected override bool TryGetCodepointByIconName(string iconName, out string codepoint)
    {
        return TryGetCodepointByIconNameFromTextAsset(
            iconName,
            "MaterialIcons-Regular",
            materialDesignIconNameToCodepointCache,
            out codepoint);
    }

    protected override string GetIconFontUssClass()
    {
        return "materialIcon";
    }
}
