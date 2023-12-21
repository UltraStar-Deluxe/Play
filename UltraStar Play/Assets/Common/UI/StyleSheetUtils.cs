using System.IO;
using OneJS.CustomStyleSheets;
using UnityEngine.UIElements;

public static class StyleSheetUtils
{
    public static StyleSheet CreateStyleSheetFromFile(string filePath)
    {
        string styleSheetContent = File.ReadAllText(filePath);
        return CreateStyleSheet(styleSheetContent);
    }

    public static StyleSheet CreateStyleSheet(string styleSheetContent)
    {
        StyleSheet styleSheet = new();
        new CustomStyleSheetImporterImpl().BuildStyleSheet(styleSheet, styleSheetContent);
        return styleSheet;
    }
}
