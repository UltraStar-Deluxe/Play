using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Themeable), true)]
public class ThemeableInspector : EditorBase
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Themeable myScript = target as Themeable;
        if (GUILayout.Button("Update Resources"))
        {
            Theme currentTheme = ThemeManager.Instance.GetCurrentTheme();
            myScript.ReloadResources(currentTheme);
        }
    }
}
