using System;
using System.IO;
using UnityEngine;

public class ThemeMeta
{
    public string AbsoluteFilePath { get; private set; }
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(AbsoluteFilePath);

    private ThemeJson themeJson;
    public ThemeJson ThemeJson
    {
        get
        {
            if (themeJson == null)
            {
                try
                {
                    string json = File.ReadAllText(AbsoluteFilePath);
                    themeJson = JsonConverter.FromJson<ThemeJson>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load theme {AbsoluteFilePath}: {e.Message}");
                    Debug.LogException(e);
                    themeJson = new();
                }
            }

            return themeJson;
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
