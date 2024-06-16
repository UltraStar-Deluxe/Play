using System;
using System.IO;
using UnityEngine;

public class ThemeMeta
{
    public string AbsoluteFilePath { get; private set; }
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsoluteFilePath);
    public string AbsoluteFolderPath => Path.GetDirectoryName(AbsoluteFilePath);
    public string FileContent => File.ReadAllText(AbsoluteFilePath);

    /**
     * The raw JSON file content as a data structure.
     */
    private ThemeJson unresolvedThemeJson;
    internal ThemeJson UnresolvedThemeJson
    {
        get
        {
            if (unresolvedThemeJson == null)
            {
                try
                {
                    string json = FileContent;
                    unresolvedThemeJson = JsonConverter.FromJson<ThemeJson>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load theme {AbsoluteFilePath}: {e.Message}");
                    Debug.LogException(e);
                    unresolvedThemeJson = new();
                }
            }

            return unresolvedThemeJson;
        }
    }

    /**
     * The resolved theme, i.e., without relative file paths and merged with content of parent themes.
     * Needs to be set externally.
     */
    public ThemeJson themeJson;
    public ThemeJson ThemeJson
    {
        get
        {
            if (themeJson == null)
            {
                return UnresolvedThemeJson;
            }
            return themeJson;
        }
        set
        {
            themeJson = value;
        }
    }

    public ThemeMeta(string absoluteFilePath)
    {
        this.AbsoluteFilePath = absoluteFilePath;
    }

    public override string ToString()
    {
        return base.ToString() + $"({FileNameWithoutExtension})";
    }
}
