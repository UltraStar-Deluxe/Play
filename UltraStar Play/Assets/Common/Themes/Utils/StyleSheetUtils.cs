using System.IO;
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
        // Not supported in UltraStar Play. Melody Mania uses OneJS to load Style Sheets at runtime.
    }

    public static StyleSheet CreateStyleSheet(string styleSheetContent)
    {
        StyleSheet styleSheet = new StyleSheet();
        BuildStyleSheet(styleSheet, styleSheetContent);
        return styleSheet;
    }
}
