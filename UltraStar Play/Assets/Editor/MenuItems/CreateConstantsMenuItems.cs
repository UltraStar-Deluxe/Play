using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreateConstantsMenuItems
{
    public static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in" };

    public static readonly string className = "R";

    private static readonly string indentation = "    ";

    [MenuItem("Resources/Create C# constants for I18N properties")]
    public static void CreateI18nConstants()
    {
        string subClassName = "String";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> i18nKeys = I18NManager.Instance.GetKeys();
        if (i18nKeys.IsNullOrEmpty())
        {
            Debug.LogWarning("No i18n keys found.");
            return;
        }

        i18nKeys.Sort();
        string classCode = CreateClassCode(subClassName, i18nKeys);
        File.WriteAllText(targetPath, classCode);
        Debug.Log("Generated file " + targetPath);
    }

    [MenuItem("Resources/Create C# constants for theme colors")]
    public static void CreateConstantsForColors()
    {
        string subClassName = "Color";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> colors = ThemeManager.Instance.GetAvailableColors();
        if (colors.IsNullOrEmpty())
        {
            Debug.LogWarning("No theme colors found.");
            return;
        }

        colors.Sort();
        string classCode = CreateClassCode(subClassName, colors);
        File.WriteAllText(targetPath, classCode);
        Debug.Log("Generated file " + targetPath);
    }

    public static string CreateClassCode(string subClassName, List<string> constantValues)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLine("public static partial class " + className + " {");
        sb.AppendLine(indentation + "public static class " + subClassName + " {");
        AppendFieldDeclarations(sb, constantValues, indentation + indentation);
        sb.AppendLine(indentation + "}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static void AppendFieldDeclarations(StringBuilder sb, List<string> values, string indentation)
    {
        values.ForEach(value =>
        {
            string fieldName = value.Replace(".", "_");
            if (cSharpKeywords.Contains(fieldName))
            {
                fieldName += "_";
            }
            sb.Append(indentation);
            sb.AppendLine($"public static readonly string {fieldName} = \"{value}\";");
        });
    }
}
