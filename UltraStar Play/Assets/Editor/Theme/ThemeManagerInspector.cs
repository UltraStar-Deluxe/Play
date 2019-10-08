using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(ThemeManger))]
public class ThemeManagerInspector : EditorBase
{

    int quickThemeSelectIndex;
    string[] quickThemeSelectItems;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (quickThemeSelectItems != null && quickThemeSelectItems.Length > 0)
        {
            int newQuickThemeSelectIndex = EditorGUILayout.Popup("Quick Theme Select", quickThemeSelectIndex, quickThemeSelectItems);
            if (newQuickThemeSelectIndex != quickThemeSelectIndex)
            {
                quickThemeSelectIndex = newQuickThemeSelectIndex;
                ThemeManger.Instance.currentThemeName = quickThemeSelectItems[quickThemeSelectIndex];
                UpdateThemeResources();
            }
        }

        if (GUILayout.Button("Update Theme Resources"))
        {
            UpdateThemeResources();
        }
    }

    private void UpdateThemeResources()
    {
        ThemeManger.Instance.UpdateThemeResources();
        UpdateQuickThemeSelect();

        // Make Themeable instances dirty, such they will be refreshed in the Unity Editor.
        Themeable[] themeables = FindObjectsOfType<Themeable>();
        foreach (Themeable themeable in themeables)
        {
            EditorUtility.SetDirty(themeable.gameObject);
        }
    }

    private void UpdateQuickThemeSelect()
    {
        List<string> loadedThemeNames = ThemeManger.Instance.GetLoadedThemeNames();
        quickThemeSelectItems = loadedThemeNames.ToArray();
        Theme currentTheme = ThemeManger.Instance.GetCurrentTheme();
        if (currentTheme != null)
        {
            quickThemeSelectIndex = loadedThemeNames.IndexOf(currentTheme.Name);
        }
    }
}
