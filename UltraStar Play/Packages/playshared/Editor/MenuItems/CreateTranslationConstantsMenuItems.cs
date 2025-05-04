using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProTrans;
using UnityEditor;
using UnityEngine;

public static class CreateTranslationConstantsMenuItems
{
    private static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in", "continue" };

    public static readonly string className = "R";

    private static readonly string indentation = "    ";

    [MenuItem("Generate/C# Constants/Translation properties")]
    public static void CreateTranslationConstants()
    {
        Translation.InitTranslationConfig();

        string generatedConstantsFolder = "Assets/Common/R";
        string subClassName = "Messages";
        string targetPath = new FileInfo($"{generatedConstantsFolder}/{className}{subClassName}.cs").FullName;

        List<string> translationKeys = GetTranslationKeys();
        if (translationKeys.IsNullOrEmpty())
        {
            Debug.LogWarning("No translation keys found.");
            return;
        }

        translationKeys.Sort();
        string classCode = CreateClassCode(subClassName, translationKeys);
        Directory.CreateDirectory(generatedConstantsFolder);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        AssetDatabase.ImportAsset(targetPath);
        Debug.Log("Generated file " + targetPath);
    }

    private static List<string> GetTranslationKeys()
    {
        HashSet<string> keys = new HashSet<string>();
        PropertiesFile propertiesFile = Translation.GetPropertiesFile(Translation.GetFallbackCultureInfo());
        if (propertiesFile == null)
        {
            return new List<string>();
        }
        keys.AddRange(propertiesFile.Dictionary.Keys);

        return keys
            .OrderBy(it => it)
            .ToList();
    }

    private static string CreateClassCode(string subClassName, List<string> constantValues, List<string> fieldNames = null)
    {
        string newline = Environment.NewLine;

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

    private static string ToUpperInvariantFirstCharacter(string s)
    {
        if (s.IsNullOrEmpty())
        {
            return string.Empty;
        }
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
