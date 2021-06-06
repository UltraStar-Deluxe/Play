using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreateConstantsMenuItems
{
    private static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in", "continue" };

    public static readonly string className = "R";

    private static readonly string indentation = "    ";

    [MenuItem("Generate/Create all C# constants")]
    public static void CreateAllConstants()
    {
        EditorUtils.RefreshAssetsInStreamingAssetsFolder();

        CreateConstantsForColors();
        CreateConstantsForImageFiles();
        CreateConstantsForAudioFiles();
        ProTrans.CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        PrimeInputActions.CreateInputActionConstantsMenuItems.CreateInputActionConstants();
    }

    [MenuItem("Generate/Create C# constants for theme colors")]
    public static void CreateConstantsForColors()
    {
        string subClassName = "Color";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> colors = ThemeManager.GetThemes()
            .SelectMany(theme => theme.LoadedColors.Keys)
            .Distinct()
            .ToList();
        if (colors.IsNullOrEmpty())
        {
            Debug.LogWarning("No theme colors found.");
            return;
        }

        colors.Sort();
        string classCode = CreateClassCode(subClassName, colors);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    [MenuItem("Generate/Create C# constants for theme files")]
    public static void CreateConstantsForFiles()
    {
        CreateConstantsForImageFiles();
        CreateConstantsForAudioFiles();
    }

    private static void CreateConstantsForAudioFiles()
    {
        string subClassName = "Audio";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> files = GetFilesInStreamingAssetsFolder("*.wav", "*.ogg");
        files.Sort();

        string classCode = CreateClassCode(subClassName, files);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    private static void CreateConstantsForImageFiles()
    {
        string subClassName = "Image";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> files = GetFilesInStreamingAssetsFolder("*.png");
        files.Sort();

        string classCode = CreateClassCode(subClassName, files);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    private static List<string> GetFilesInStreamingAssetsFolder(params string[] fileExtensions)
    {
        List<string> result = new List<string>();
        foreach (string fileExtension in fileExtensions)
        {
            string[] files = Directory.GetFiles("Assets/StreamingAssets/", fileExtension, SearchOption.AllDirectories);
            foreach (string file in files)
            {
                result.Add(GetFilePathRelativeToThemeFolder(file));
            }
        }
        return result.Distinct().ToList();
    }

    private static string GetFilePathRelativeToThemeFolder(string file)
    {
        string themesFolderPrefix = "Assets/StreamingAssets/" + ThemeManager.ThemesFolderName + "/";

        string result = file.Replace("\\", "/");
        int indexOfThemesFolder = result.IndexOf(themesFolderPrefix, StringComparison.CurrentCulture);
        result = result.Substring(indexOfThemesFolder + themesFolderPrefix.Length);
        int indexOfNextFolder = result.IndexOf("/", StringComparison.CurrentCulture);
        result = result.Substring(indexOfNextFolder + 1);

        return result;
    }

    private static string CreateClassCode(string subClassName, List<string> constantValues, List<string> fieldNames = null)
    {
        string newline = System.Environment.NewLine;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLine("public static partial class " + className + newline + "{");
        sb.AppendLine(indentation + "public static class " + subClassName + newline + indentation + "{");
        AppendFieldDeclarations(sb, constantValues, fieldNames, indentation + indentation);
        sb.AppendLine(indentation + "}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendFieldDeclarations(StringBuilder sb, List<string> values, List<string> fieldNames, string indentation)
    {
        for(int i = 0; i < values.Count; i++)
        {
            string value = values[i];
            string fieldName = fieldNames == null
                ? value.Replace(".", "_")
                : fieldNames[i];
            if (fieldName.Contains("/"))
            {
                fieldName = Path.GetFileNameWithoutExtension(fieldName);
            }
            if (cSharpKeywords.Contains(fieldName))
            {
                fieldName += "_";
            }

            sb.Append(indentation);
            sb.AppendLine($"public static readonly string {fieldName} = \"{value}\";");
        }
    }
}
