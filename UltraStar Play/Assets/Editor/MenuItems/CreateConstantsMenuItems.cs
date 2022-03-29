using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

        CreateConstantsForUxmlNamesAndClasses();
        ProTrans.CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        PrimeInputActions.CreateInputActionConstantsMenuItems.CreateInputActionConstants();
    }

    [MenuItem("Generate/Create C# constants for UXML names and classes")]
    private static void CreateConstantsForUxmlNamesAndClasses()
    {
        CreateConstantsForUxmlNames();
        CreateConstantsForUxmlClasses();
    }

    private static void CreateConstantsForUxmlNames()
    {
        string subClassName = "UxmlNames";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> files = GetFilesInFolder("Assets", "*.uxml");

        HashSet<string> uxmlNames = new HashSet<string>();
        files.ForEach(file => FindUxmlNames(file).ForEach(uxmlName => uxmlNames.Add(uxmlName)));
        List<string> uxmlNamesList = uxmlNames.ToList();
        uxmlNamesList.Sort();

        string classCode = CreateClassCode(subClassName, uxmlNamesList, null, true);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    private static void CreateConstantsForUxmlClasses()
    {
        string subClassName = "UxmlClasses";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> files = GetFilesInFolder("Assets", "*.uxml");

        HashSet<string> uxmlClasses = new HashSet<string>();
        files.ForEach(file => FindUxmlClasses(file).ForEach(uxmlClass => uxmlClasses.Add(uxmlClass)));
        List<string> uxmlClassesList = uxmlClasses.ToList();
        uxmlClassesList.Sort();

        string classCode = CreateClassCode(subClassName, uxmlClassesList, null, true);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    private static IEnumerable<string> FindUxmlNames(string uxmlFile)
    {
        HashSet<string> result = new HashSet<string>();
        string content = File.ReadAllText(uxmlFile);
        XElement xroot = XElement.Parse(content);
        xroot.DescendantsAndSelf()
            .ForEach(xelement =>
            {
                string nameAttribute = xelement.Attribute("name").String();
                if (!nameAttribute.IsNullOrEmpty())
                {
                    result.Add(nameAttribute.Trim());
                }
            });
        return result;
    }

    private static IEnumerable<string> FindUxmlClasses(string uxmlFile)
    {
        HashSet<string> result = new HashSet<string>();
        string content = File.ReadAllText(uxmlFile);
        XElement xroot = XElement.Parse(content);
        xroot.DescendantsAndSelf()
            .ForEach(xelement =>
            {
                string classAttributeList = xelement.Attribute("class").String();
                if (!classAttributeList.IsNullOrEmpty())
                {
                    string[] classAttributes = classAttributeList.Split(" ");
                    classAttributes.ForEach(classAttribute => result.Add(classAttribute.Trim()));
                }
            });
        return result;
    }

    private static List<string> GetFilesInFolder(string folderPath, params string[] fileExtensions)
    {
        List<string> result = new List<string>();
        foreach (string fileExtension in fileExtensions)
        {
            string[] files = Directory.GetFiles(folderPath, fileExtension, SearchOption.AllDirectories);
            result.AddRange(files);
        }
        return result.Distinct().ToList();
    }

    private static string CreateClassCode(string subClassName, List<string> constantValues, List<string> fieldNames = null, bool useConst = false)
    {
        string newline = System.Environment.NewLine;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLine("public static partial class " + className + newline + "{");
        sb.AppendLine(indentation + "public static class " + subClassName + newline + indentation + "{");
        AppendFieldDeclarations(sb, constantValues, fieldNames, indentation + indentation, useConst);
        sb.AppendLine(indentation + "}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendFieldDeclarations(StringBuilder sb, List<string> values, List<string> fieldNames, string indentation, bool useConst = false)
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
            if (useConst)
            {
                sb.AppendLine($"public const string {fieldName} = \"{value}\";");
            }
            else
            {
                sb.AppendLine($"public static readonly string {fieldName} = \"{value}\";");
            }
        }
    }
}
