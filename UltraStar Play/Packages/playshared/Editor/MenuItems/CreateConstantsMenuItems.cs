using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PrimeInputActions;
using UnityEditor;

public static class CreateConstantsMenuItems
{
    private static readonly Regex ussClassDeclarationRegex = new Regex(@"\.(?<ussClassName>[^\d][\w\-]+)");

    private static readonly HashSet<string> cSharpKeywords = new()
    { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in", "continue" };

    private static readonly string indentation = "    ";

    [MenuItem("Generate/Create all C# constants")]
    public static void CreateAllConstants()
    {
        EditorUtils.RefreshAssetsInStreamingAssetsFolder();

        CreateConstantsForUxmlNamesAndUssClasses();
        CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        CreateInputActionConstantsMenuItems.CreateInputActionConstants();
    }

    [MenuItem("Generate/Create C# constants for UXML names and classes")]
    public static void CreateConstantsForUxmlNamesAndUssClasses()
    {
        CreateConstantsForUxmlNames();
        CreateConstantsForUssClasses();
    }

    public static void CreateConstantsForUxmlNames()
    {
        CreateConstantsForUxmlNamesInPlayShared();
        CreateConstantsForUxmlNamesInAssets();
    }

    public static void CreateConstantsForUxmlNamesInAssets()
    {
        string className = "R";
        string subClassName = "UxmlNames";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";
        List<string> uxmlFiles = GetFilesInFolderRecursive("Assets", "*.uxml")
            .ToList();

        HashSet<string> uxmlNames = new();
        uxmlFiles.ForEach(file => FindUxmlNames(file).ForEach(uxmlName => uxmlNames.Add(uxmlName)));
        List<string> uxmlNamesList = uxmlNames.ToList();
        uxmlNamesList.Sort();

        string classCode = CreateClassCode(className, subClassName, uxmlNamesList, null, true);
        FileUtils.WriteAllTextIfChanged(targetPath, classCode);
    }

    public static void CreateConstantsForUxmlNamesInPlayShared()
    {
        string className = "R_PlayShared";
        string subClassName = "UxmlNames";
        string targetPath = $"Packages/playsharedui/Runtime/R/{className + subClassName}.cs";
        string targetDirectory = Path.GetDirectoryName(targetPath);
        if (!Directory.Exists(targetDirectory))
        {
            // The PlayShared folder only exists in the main game project.
            return;
        }

        List<string> uxmlFiles = GetFilesInFolderRecursive("Packages/playsharedui/Runtime", "*.uxml")
            .ToList();

        HashSet<string> uxmlNames = new();
        uxmlFiles.ForEach(file => FindUxmlNames(file).ForEach(uxmlName => uxmlNames.Add(uxmlName)));
        List<string> uxmlNamesList = uxmlNames.ToList();
        uxmlNamesList.Sort();

        string classCode = CreateClassCode(className, subClassName, uxmlNamesList, null, true);
        FileUtils.WriteAllTextIfChanged(targetPath, classCode);
    }

    public static void CreateConstantsForUssClasses()
    {
        CreateConstantsForUssClassesInPlayShared();
        CreateConstantsForUssClassesInAssets();
    }

    public static void CreateConstantsForUssClassesInAssets()
    {
        string className = "R";
        string subClassName = "UssClasses";
        string targetPath = $"Assets/Common/R/{className + subClassName}.cs";

        List<string> uxmlFiles = GetFilesInFolderRecursive("Assets", "*.uxml")
            .ToList();
        List<string> ussFiles = GetFilesInFolderRecursive("Assets", "*.uss")
            .ToList();

        HashSet<string> ussClassesHashSet = new();
        uxmlFiles.ForEach(file => FindUssClassesInUxmlFile(file).ForEach(ussClassName => ussClassesHashSet.Add(ussClassName)));
        ussFiles.ForEach(file => FindUssClassesInUssFile(file).ForEach(ussClassName => ussClassesHashSet.Add(ussClassName)));
        List<string> ussClassesList = ussClassesHashSet.ToList();
        ussClassesList.Sort();

        string classCode = CreateClassCode(className, subClassName, ussClassesList, null, true);
        FileUtils.WriteAllTextIfChanged(targetPath, classCode);
    }

    public static void CreateConstantsForUssClassesInPlayShared()
    {
        string className = "R_PlayShared";
        string subClassName = "UssClasses";
        string targetPath = $"Packages/playsharedui/Runtime/R/{className + subClassName}.cs";
        string targetDirectory = Path.GetDirectoryName(targetPath);
        if (!Directory.Exists(targetDirectory))
        {
            // The PlayShared folder only exists in the main game project.
            return;
        }

        List<string> uxmlFiles = GetFilesInFolderRecursive("Packages/playsharedui/Runtime", "*.uxml")
            .ToList();
        List<string> ussFiles = GetFilesInFolderRecursive("Packages/playsharedui/Runtime", "*.uss")
            .ToList();

        HashSet<string> ussClassesHashSet = new();
        uxmlFiles.ForEach(file => FindUssClassesInUxmlFile(file).ForEach(ussClassName => ussClassesHashSet.Add(ussClassName)));
        ussFiles.ForEach(file => FindUssClassesInUssFile(file).ForEach(ussClassName => ussClassesHashSet.Add(ussClassName)));
        List<string> ussClassesList = ussClassesHashSet.ToList();
        ussClassesList.Sort();

        string classCode = CreateClassCode(className, subClassName, ussClassesList, null, true);
        FileUtils.WriteAllTextIfChanged(targetPath, classCode);
    }

    private static IEnumerable<string> FindUxmlNames(string uxmlFile)
    {
        HashSet<string> result = new();
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

    private static IEnumerable<string> FindUssClassesInUxmlFile(string uxmlFile)
    {
        HashSet<string> result = new();
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

    private static IEnumerable<string> FindUssClassesInUssFile(string ussFile)
    {
        HashSet<string> result = new();
        IEnumerable<string> lines = File.ReadLines(ussFile);
        lines.ForEach(line =>
        {
            Match match = ussClassDeclarationRegex.Match(line);
            if (match.Success)
            {
                string ussClassName = match.Groups["ussClassName"].Value;
                result.Add(ussClassName);
            }
        });
        return result;
    }

    private static List<string> GetFilesInFolderRecursive(string folderPath, params string[] fileExtensions)
    {
        return DirectoryUtils.GetFiles(folderPath, true, fileExtensions)
            .Where(file => !IsFileIgnored(file))
            .ToList();
    }

    private static bool IsFileIgnored(string file)
    {
        if (Path.GetFileName(file) == "UtilityStyles.uss")
        {
            return true;
        }

        return false;
    }

    private static string CreateClassCode(string className, string subClassName, List<string> constantValues, List<string> fieldNames = null, bool useConst = false)
    {
        string newline = Environment.NewLine;

        StringBuilder sb = new();
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
                ? value.Replace(".", "_").Replace("-", "_")
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
