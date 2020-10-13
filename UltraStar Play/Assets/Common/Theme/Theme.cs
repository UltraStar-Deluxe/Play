using System;
using System.Collections.Generic;
using UnityEngine;

public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    private readonly Dictionary<string, Color32> loadedColors = new Dictionary<string, Color32>();
    public IReadOnlyDictionary<string, Color32> LoadedColors => loadedColors;

    public Theme(string name, Theme parentTheme)
    {
        Name = name;
        ParentTheme = parentTheme;

        LoadColors();
    }

    private void LoadColors()
    {
        loadedColors.Clear();
        TextAsset textAsset = FindResource<TextAsset>(ThemeManager.colorsFileBaseName);
        if (textAsset != null)
        {
            Dictionary<string, string> loadedColorHexValues = PropertiesFileParser.ParseText(textAsset.text);
            loadedColorHexValues.ForEach(entry =>
            {
                Color32 loadedColor = Colors.CreateColor(entry.Value);
                loadedColors.Add(entry.Key, loadedColor);
            });
        }
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

    /// Looks for the resource with the given name in the specified theme and all parent themes.
    /// Returns null if the resource was not found. Otherwise the loaded resource is returned.
    public T FindResource<T>(string resourceBaseName) where T : UnityEngine.Object
    {
        string resourcePath = GetResourcePath(resourceBaseName);
        T asset = Resources.Load<T>(resourcePath);
        if (asset != null)
        {
            return asset;
        }
        if (ParentTheme != null)
        {
            return ParentTheme.FindResource<T>(resourceBaseName);
        }
        Debug.LogError("Could not load resource: " + resourcePath);
        return null;
    }

    private string GetResourcePath(string resourceBaseName)
    {
        return ThemeManager.themesFolderName + "/" + Name + "/" + resourceBaseName;
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
