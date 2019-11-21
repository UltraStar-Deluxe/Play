using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreateConstantsForI18NProperties
{
    private static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in" };

    [MenuItem("I18N/Create C# constants for I18N properties")]
    public static void SetSizeToAnchors()
    {
        string className = "I18NKeys";
        string targetPath = $"Assets/Common/I18N/{className}.cs";
        List<string> i18nKeys = I18NManager.Instance.GetKeys();
        if (i18nKeys.IsNullOrEmpty())
        {
            Debug.LogWarning("No i18n keys found.");
            return;
        }

        i18nKeys.Sort();

        // Create the code
        string indentation = "    ";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// Generated code with the keys of the I18N properties.");
        sb.AppendLine("// To update this file use the corresponding menu item in the Unity Editor under 'I18N'.");
        sb.AppendLine("public static class " + className + " {");
        sb.AppendFieldDeclarations(i18nKeys, indentation);
        sb.AppendLine("}");

        // Flush to file
        File.WriteAllText(targetPath, sb.ToString());
    }

    private static void AppendFieldDeclarations(this StringBuilder sb, List<string> i18nKeys, string indentation)
    {
        foreach (string key in i18nKeys)
        {
            string fieldName = key.Replace(".", "_");
            if (cSharpKeywords.Contains(fieldName))
            {
                fieldName += "_";
            }
            sb.Append(indentation);
            sb.AppendLine($"public static readonly string {fieldName} = \"{key}\";");
        }
    }

    private static void AppendLine(this StringBuilder sb, string line)
    {
        sb.Append(line);
        sb.Append("\n");
    }
}
