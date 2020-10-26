using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Themeable), true)]
public class ThemeableInspector : EditorBase
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Themeable themeable = target as Themeable;
        if (GUILayout.Button("Update Resources"))
        {
            themeable.ReloadResources(ThemeManager.CurrentTheme);
            EditorUtility.SetDirty(themeable.gameObject);
        }
    }
}
