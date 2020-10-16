using System.Collections.Generic;
using UnityEngine;

public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    private readonly Dictionary<string, Color32> loadedColors = new Dictionary<string, Color32>();
    public IReadOnlyDictionary<string, Color32> LoadedColors => loadedColors;

    public bool HasFinishedLoadingColors { get; private set; }

    public Theme(string name, Theme parentTheme, CoroutineManager coroutineManager)
    {
        Name = name;
        ParentTheme = parentTheme;

        LoadColors(coroutineManager);
    }

    private void LoadColors(CoroutineManager coroutineManager)
    {
        loadedColors.Clear();
        string colorsFileUri = GetStreamingAssetsUri(ThemeManager.colorsFileName);
        coroutineManager.StartCoroutineAlsoForEditor(WebRequestUtils.LoadTextFromUri(colorsFileUri,
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
        HasFinishedLoadingColors = true;
        Debug.Log(Name + " finished loading colors");
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

    public string GetStreamingAssetsUri(string resourceName)
    {
        string resourcePath = ThemeManager.themesFolderName + "/" + Name + "/" + resourceName;
        return ApplicationUtils.GetStreamingAssetsUri(resourcePath);
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
