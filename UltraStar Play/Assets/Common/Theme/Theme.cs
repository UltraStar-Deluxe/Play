using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ProTrans;

public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    private Dictionary<string, string> loadedColorRawStringValues;
    private readonly Dictionary<string, Color32> loadedColors = new Dictionary<string, Color32>();
    public IReadOnlyDictionary<string, Color32> LoadedColors => loadedColors;

    private AudioManager audioManager;

    public Theme(string name, Theme parentTheme)
    {
        Name = name;
        ParentTheme = parentTheme;

        LoadColors();
    }

    /**
     * Looks for the AudioClip on the given path in this theme and all parent themes.
     * Returns null if it could not be loaded.
     */
    public AudioClip LoadAudioClip(string audioPath)
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        string fullAudioPath = GetStreamingAssetsPath(audioPath);
        AudioClip audioClip = null;
        if (File.Exists(fullAudioPath))
        {
            audioClip = audioManager.LoadAudioClipFromFile(fullAudioPath);
        }
        else if (ParentTheme != null)
        {
            audioClip = ParentTheme.LoadAudioClip(audioPath);
        }
        return audioClip;
    }

    /**
     * Looks for the Sprite on the given path in this theme and all parent themes.
     * Returns null if it could not be loaded.
     */
    public Sprite LoadSprite(string imagePath)
    {
        string fullImagePath = GetStreamingAssetsPath(imagePath);
        Sprite sprite = null;
        if (File.Exists(fullImagePath))
        {
            return null;
        }
        else if (ParentTheme != null)
        {
            sprite = ParentTheme.LoadSprite(imagePath);
        }
        return sprite;
    }

    /**
     * Looks for the color with the given name in the current theme and all parent themes.
     * Returns true iff the color was found.
     */
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

    /**
     * Looks for the color with the given name in the current theme and all parent themes.
     * Returns true iff the color was found.
     */
    private bool TryFindColorRawStringValue(string colorName, out string colorValue)
    {
        if (loadedColorRawStringValues != null
            && loadedColorRawStringValues.TryGetValue(colorName, out colorValue))
        {
            return true;
        }
        else if (ParentTheme != null)
        {
            return ParentTheme.TryFindColorRawStringValue(colorName, out colorValue);
        }
        colorValue = "";
        return false;
    }

    private string GetStreamingAssetsPath(string resourceName)
    {
        string resourcePath = ThemeManager.ThemesFolderName + "/" + Name + "/" + resourceName;
        return ApplicationUtils.GetStreamingAssetsPath(resourcePath);
    }


    private void LoadColors()
    {
        loadedColors.Clear();
        string colorsFilePath = GetStreamingAssetsPath(ThemeManager.ColorsFileName);
        string colorsFileContent = File.ReadAllText(colorsFilePath);
        LoadColorsFromText(colorsFileContent);
    }

    private void LoadColorsFromText(string text)
    {
        loadedColorRawStringValues = PropertiesFileParser.ParseText(text);
        loadedColorRawStringValues.ForEach(entry =>
        {
            // Replace reference to other color
            string colorHexValue = ReplaceColorReferences(entry.Value);
            Color32 loadedColor = Colors.CreateColor(colorHexValue);
            loadedColors.Add(entry.Key, loadedColor);
        });
    }

    private string ReplaceColorReferences(string colorValueString)
    {
        if (colorValueString.StartsWith("$"))
        {
            string otherColorVariableName = colorValueString.Substring(1);
            // Lookup in the colors of this Theme
            if (loadedColorRawStringValues.TryGetValue(otherColorVariableName, out string otherColorValueFromThisTheme))
            {
                // Recursively replace further color references
                return ReplaceColorReferences(otherColorValueFromThisTheme);
            }
            // Lookup in ParentTheme
            if (ParentTheme.TryFindColorRawStringValue(otherColorVariableName, out string otherColorValueFromParentTheme))
            {
                // Recursively replace further color references
                return ReplaceColorReferences(otherColorValueFromParentTheme);
            }
        }
        return colorValueString;
    }

    public override string ToString()
    {
        if (ParentTheme != null)
        {
            return $"{Name} extends {ParentTheme.Name}";
        }
        else
        {
            return $"{Name}";
        }
    }
}
