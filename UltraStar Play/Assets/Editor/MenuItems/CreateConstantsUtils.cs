using System;
using System.Collections.Generic;
using System.Text;

public static class CreateConstantsUtils
{
    public static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in" };

    public static readonly string className = "R";

    private static readonly string indentation = "    ";

    public static string CreateClassCode(string subClassName, List<string> constantValues)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLine("public static partial class " + CreateConstantsUtils.className + " {");
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
