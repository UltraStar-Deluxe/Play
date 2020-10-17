using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(ThemeManager))]
public class ThemeManagerInspector : EditorBase
{
    int quickThemeSelectIndex;
    string[] quickThemeSelectItems;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (ThemeManager.HasFinishedLoadingThemes
            && quickThemeSelectItems == null || quickThemeSelectItems.Length == 0)
        {
            quickThemeSelectItems = ThemeManager.GetThemeNames().ToArray();
        }

        if (quickThemeSelectItems != null && quickThemeSelectItems.Length > 0)
        {
            int newQuickThemeSelectIndex = EditorGUILayout.Popup("Quick Theme Select", quickThemeSelectIndex, quickThemeSelectItems);
            if (newQuickThemeSelectIndex != quickThemeSelectIndex)
            {
                quickThemeSelectIndex = newQuickThemeSelectIndex;
                ThemeManager.CurrentTheme = ThemeManager.GetTheme(quickThemeSelectItems[quickThemeSelectIndex]);
                UpdateThemeResources();
            }
        }

        if (GUILayout.Button("Update Theme Components in Scene"))
        {
            UpdateThemeResources();
        }

        if (GUILayout.Button("Reload Themes"))
        {
            ThemeManager.Instance.ReloadThemes();
        }
    }

    private void UpdateThemeResources()
    {
        // Update the themes
        ThemeManager.Instance.UpdateThemeResources();
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
        List<string> loadedThemeNames = ThemeManager.GetThemeNames();
        quickThemeSelectItems = loadedThemeNames.ToArray();
        Theme currentTheme = ThemeManager.CurrentTheme;
        if (currentTheme != null)
        {
            quickThemeSelectIndex = loadedThemeNames.IndexOf(currentTheme.Name);
        }
    }
}
