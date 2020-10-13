using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreateConstantsForThemeableColors
{
    [MenuItem("Resources/Create C# constants for theme colors")]
    public static void SetSizeToAnchors()
    {
        string subClassName = "Color";
        string targetPath = $"Assets/Common/R/{CreateConstantsUtils.className + subClassName}.cs";

        List<string> colors = ThemeManager.Instance.GetAvailableColors();
        if (colors.IsNullOrEmpty())
        {
            Debug.LogWarning("No theme colors found.");
            return;
        }

        colors.Sort();
        string classCode = CreateConstantsUtils.CreateClassCode(subClassName, colors);
        File.WriteAllText(targetPath, classCode);
        Debug.Log("Generated file " + targetPath);
    }
}
