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

    public static void BuildStyleSheet(StyleSheet styleSheet, string styleSheetContent)
    {
        new CustomStyleSheetImporterImpl().BuildStyleSheet(styleSheet, styleSheetContent);
    }

    public static StyleSheet CreateStyleSheet(string styleSheetContent)
    {
        StyleSheet styleSheet = new StyleSheet();
        BuildStyleSheet(styleSheet, styleSheetContent);
        return styleSheet;
    }
}
