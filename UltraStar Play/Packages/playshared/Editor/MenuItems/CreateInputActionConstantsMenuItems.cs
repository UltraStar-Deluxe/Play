using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CreateInputActionConstantsMenuItem
{
    private static HashSet<string> cSharpKeywords = new HashSet<string> { "public", "protected", "private",
        "static", "void", "readonly", "const",
        "using", "class", "enum", "interface", "new", "this", "override", "virtual",
        "string", "int", "float", "double", "short", "long", "bool",
        "null", "true", "false", "out", "ref",
        "get", "set", "if", "else", "while", "return", "do", "for", "foreach", "in", "continue" };

    public static readonly string className = "R";

    private static readonly string indentation = "    ";

    [MenuItem("Generate/C# Constants/InputActions")]
    public static void CreateInputActionConstants()
    {
        PrimeInputActions.InputManager inputManager = PrimeInputActions.InputManager.Instance;
        if (inputManager == null)
        {
            Debug.LogError("No InputManager found. Not creating InputAction paths constants.");
            return;
        }

        string subClassName = "InputActions";
        string targetPath = $"{inputManager.generatedConstantsFolder}/{className + subClassName}.cs";

        List<string> inputActionPaths = PrimeInputActions.InputManager.Instance.defaultInputActionAsset.actionMaps
            .SelectMany(actionMap => actionMap.actions)
            .Select(action => action.actionMap.name + "/" + action.name)
            .ToList();
        inputActionPaths.Sort();

        List<string> fieldNames = inputActionPaths
            .Select(it => it.Replace("/", "_"))
            .ToList();

        string classCode = CreateClassCode(subClassName, inputActionPaths, fieldNames);
        Directory.CreateDirectory(inputManager.generatedConstantsFolder);
        File.WriteAllText(targetPath, classCode, Encoding.UTF8);

        AssetDatabase.ImportAsset(targetPath);
        if (inputManager.LogInfoNow)
        {
            Debug.Log("Generated file " + targetPath);
        }
    }

    private static string CreateClassCode(string subClassName, List<string> constantValues, List<string> fieldNames = null)
    {
        string newline = System.Environment.NewLine;

        StringBuilder sb = new StringBuilder();
        sb.AppendLineFeed("// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLineFeed("public static partial class " + className + newline + "{");
        sb.AppendLineFeed(indentation + "public static class " + subClassName + newline + indentation + "{");
        AppendFieldDeclarations(sb, constantValues, fieldNames, indentation + indentation);
        sb.AppendLineFeed(indentation + "}");
        sb.AppendLineFeed("}");
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
            sb.AppendLineFeed($"public static readonly string {fieldName} = \"{value}\";");
        }
    }
}
