using System.Collections.Generic;
using UnityEngine.UIElements;

public class MaterialIcon : FontIcon
{
    private static readonly Dictionary<string, string> materialDesignIconNameToCodepointCache = new();

    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<MaterialIcon, UxmlTraits> {}

    public new class UxmlTraits : FontIcon.UxmlTraits {}

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
