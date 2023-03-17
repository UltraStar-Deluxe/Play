using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BootstrapIcon : FontIcon
{
    private static readonly Dictionary<string, string> bootstrapIconNameToCodepointCache = new();

    // UIToolkit factory classes
    public new class UxmlFactory : UxmlFactory<BootstrapIcon, UxmlTraits> {}

    public new class UxmlTraits : FontIcon.UxmlTraits {}

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
