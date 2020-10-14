using System;
using System.Collections.Generic;
using UnityEngine;

public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    private readonly Dictionary<string, Color32> loadedColors = new Dictionary<string, Color32>();
    public IReadOnlyDictionary<string, Color32> LoadedColors => loadedColors;

    public Theme(string name, Theme parentTheme, ThemeManager themeManager)
    {
        Name = name;
        ParentTheme = parentTheme;

        LoadColors(themeManager);
    }

    private void LoadColors(ThemeManager themeManager)
    {
        loadedColors.Clear();
        string colorsFileUri = ApplicationUtils.GetStreamingAssetsUri(GetResourcePath(ThemeManager.colorsFileName));
        themeManager.StartCoroutine(WebRequestUtils.LoadTextFromUri(colorsFileUri,
            (loadedText) => LoadColorsFromText(loadedText)));
    }

    private void LoadColorsFromText(string text)
    {
        Dictionary<string, string> loadedColorHexValues = PropertiesFileParser.ParseText(text);
        loadedColorHexValues.ForEach(entry =>
        {
            Color32 loadedColor = Colors.CreateColor(entry.Value);
            loadedColors.Add(entry.Key, loadedColor);
        });
    }

    /// Looks for the color with the given name in the current theme and all parent themes.
    /// Returns true iff the color was found.
    public bool TryFindColor(string colorName, out Color32 resultColor)
    {
        if (loadedColors.TryGetValue(colorName, out resultColor))
        {
            return true;
        }
        else if (ParentTheme != null)
        {
            return ParentTheme.TryFindColor(colorName, out resultColor);
        }
        resultColor = Colors.white;
        return false;
    }

    private string GetResourcePath(string resourceName)
    {
        return ThemeManager.themesFolderName + "/" + Name + "/" + resourceName;
    }

    public override string ToString()
    {
        if (ParentTheme != null)
        {
            return $"{Name}<{ParentTheme.Name}";
        }
        else
        {
            return $"{Name}";
        }
    }
}
