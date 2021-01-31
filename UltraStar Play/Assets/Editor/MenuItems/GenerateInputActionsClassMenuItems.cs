using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GenerateInputActionsClassMenuItems
{
    public static readonly string className = "InputActions";

    private static readonly int indentWidth = 4;

    [MenuItem("Generate/Create InputActions class")]
    public static void CreateInputActionsClass()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.");
        sb.AppendLine("using UnityEngine.InputSystem;\n");
        sb.AppendLine("public class InputActions");
        sb.AppendLine("{");
        InputManager.Instance.defaultInputActionAsset.actionMaps.ForEach(actionMap => sb.AppendLine($"public {actionMap.name}InputActions {actionMap.name} {{ get; private set; }}", indentWidth));
        sb.AppendLine("");
        sb.AppendLine("public InputActions(InputActionAsset inputActionAsset)", indentWidth);
        sb.AppendLine("{", indentWidth);
        InputManager.Instance.defaultInputActionAsset.actionMaps.ForEach(actionMap => sb.AppendLine($"{actionMap.name} = new {actionMap.name}InputActions(inputActionAsset);", indentWidth*2));
        sb.AppendLine("}", indentWidth);
        sb.AppendLine("");
        InputManager.Instance.defaultInputActionAsset.actionMaps.ForEach(actionMap => AppendInputActionMapClass(sb, actionMap));
        sb.AppendLine("}");

        string targetPath = "Assets/Common/Input/InputActions.cs";
        File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("Generated file " + targetPath);
    }

    private static void AppendInputActionMapClass(StringBuilder sb, InputActionMap actionMap)
    {
        string subClassName = $"{actionMap.name}InputActions";
        sb.AppendLine($"public class {subClassName}", indentWidth);
        sb.AppendLine("{", indentWidth);
        actionMap.actions.ForEach(action => sb.AppendLine($"public InputAction {action.name}Action {{ get; private set; }}", indentWidth*2));
        sb.AppendLine("");
        sb.AppendLine($"public {subClassName}(InputActionAsset inputActionAsset)", indentWidth*2);
        sb.AppendLine("{", indentWidth*2);
        actionMap.actions.ForEach(action => sb.AppendLine($"{action.name}Action = inputActionAsset.FindAction(\"{actionMap.name}/{action.name}\", true);", indentWidth*3));
        sb.AppendLine("}", indentWidth*2);
        sb.AppendLine("}", indentWidth);
        sb.AppendLine("");
    }
}
