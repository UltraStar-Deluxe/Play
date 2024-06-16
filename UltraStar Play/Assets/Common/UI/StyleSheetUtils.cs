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

    public static StyleSheet CreateStyleSheet(StyleSheet styleSheet, string styleSheetContent)
    {
        return new CustomStyleSheetImporterImpl().BuildStyleSheet(styleSheet, styleSheetContent);
    }

    public static StyleSheet CreateStyleSheet(string styleSheetContent)
    {
        return CreateStyleSheet(new StyleSheet(), styleSheetContent);
    }
}
