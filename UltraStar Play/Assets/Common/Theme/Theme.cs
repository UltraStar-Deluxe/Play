using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Theme
{
    public string Name { get; set; }
    public Theme ParentTheme { get; private set; }

    private Dictionary<string, string> loadedColorRawStringValues;
    private readonly Dictionary<string, Color32> loadedColors = new Dictionary<string, Color32>();
    public IReadOnlyDictionary<string, Color32> LoadedColors => loadedColors;

    public bool HasFinishedLoadingColors { get; private set; }

    private AudioManager audioManager;

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
        loadedColorRawStringValues = PropertiesFileParser.ParseText(text);
        loadedColorRawStringValues.ForEach(entry =>
        {
            // Replace reference to other color
            string colorHexValue = ReplaceColorReferences(entry.Value);
            Color32 loadedColor = Colors.CreateColor(colorHexValue);
            loadedColors.Add(entry.Key, loadedColor);
        });


        HasFinishedLoadingColors = true;
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

    internal void LoadAudioClip(string audioPath, Action<AudioClip> onSuccess)
    {
        void OnFailure(UnityWebRequest webRequest)
        {
            if (ParentTheme != null)
            {
                ParentTheme.LoadAudioClip(audioPath, onSuccess);
                return;
            }
            Debug.LogWarning("Could not load theme file: " + audioPath);
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }
        audioManager.LoadAudioClipFromUri(GetStreamingAssetsUri(audioPath),
                onSuccess,
                OnFailure);
    }

    public void LoadSprite(string imagePath, Action<Sprite> onSuccess)
    {
        void OnFailure(UnityWebRequest webRequest)
        {
            if (ParentTheme != null)
            {
                ParentTheme.LoadSprite(imagePath, onSuccess);
                return;
            }
            Debug.LogWarning("Could not load theme file: " + imagePath);
        }

        ImageManager.LoadSpriteFromUri(GetStreamingAssetsUri(imagePath),
                onSuccess,
                OnFailure);
    }

    public void LoadText(string textFilePath, Action<string> onSuccess)
    {
        void OnFailure(UnityWebRequest webRequest)
        {
            if (ParentTheme != null)
            {
                ParentTheme.LoadText(textFilePath, onSuccess);
                return;
            }
            Debug.LogWarning("Could not load theme file: " + textFilePath);
        }

        WebRequestUtils.LoadTextFromUri(GetStreamingAssetsUri(textFilePath),
                onSuccess,
                OnFailure);
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

    /// Looks for the color with the given name in the current theme and all parent themes.
    /// Returns true iff the color was found.
    private bool TryFindColorRawStringValue(string colorName, out string colorValue)
    {
        if (loadedColorRawStringValues.TryGetValue(colorName, out colorValue))
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
