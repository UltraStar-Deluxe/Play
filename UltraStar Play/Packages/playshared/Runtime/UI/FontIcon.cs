using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class FontIcon : Label
{
    private static string missingIconText = "?";

    public string icon;

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // Additional XML attributes
        private readonly UxmlStringAttributeDescription icon = new() { name = "icon", defaultValue = "" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            FontIcon target = ve as FontIcon;

            // Read additional attributes from XML
            target.icon = icon.GetValueFromBag(bag, cc);

            // Set USS classes and text (codepoint of the icon) to render the icon from a Font Asset.
            if (string.IsNullOrEmpty(target.icon))
            {
                target.text = "";
            }
            else
            {
                if (target.TryGetCodepointByIconName(target.icon, out string codepoint))
                {
                    target.AddToClassList("fontIcon");
                    target.AddToClassList(target.GetIconFontUssClass());
                    target.text = "\\u" + codepoint;
                }
                else
                {
                    target.text = missingIconText;
                }
            }
        }
    }

    protected abstract bool TryGetCodepointByIconName(string iconName, out string codepoint);
    protected abstract string GetIconFontUssClass();

    protected static bool TryGetCodepointByIconNameFromTextAsset(
        string iconName,
        string textAssetPath,
        Dictionary<string, string> iconNameToCodepointCache,
        out string codepoint)
    {
        // Lazy load mapping
        if (iconNameToCodepointCache.Count == 0)
        {
            TextAsset codepointsTextAsset = Resources.Load<TextAsset>(textAssetPath);
            if (codepointsTextAsset == null)
            {
                Debug.LogError($"Codepoints TextAsset not found using path '{textAssetPath}'");
                codepoint = "";
                return false;
            }

            // Expected format of the TextAsset is one mapping per line.
            // Each line starts with the icon name, followed by a space, followed by the codepoint.
            string[] codepointLines = codepointsTextAsset.text.Split("\n");
            codepointLines.ForEach(codepointLine =>
            {
                string[] iconNameAndCodepoint = codepointLine.Trim().Split(" ");
                if (iconNameAndCodepoint.Length == 2)
                {
                    string iconNameInLine = iconNameAndCodepoint[0];
                    string codepointInLine = iconNameAndCodepoint[1];
                    iconNameToCodepointCache[iconNameInLine] = codepointInLine;
                }
            });
        }

        if (iconNameToCodepointCache.TryGetValue(iconName, out string cachedCodepoint))
        {
            codepoint = cachedCodepoint;
            return true;
        }

        codepoint = "";
        return false;
    }
}
