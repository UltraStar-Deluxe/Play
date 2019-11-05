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

        if (quickThemeSelectItems != null && quickThemeSelectItems.Length > 0)
        {
            int newQuickThemeSelectIndex = EditorGUILayout.Popup("Quick Theme Select", quickThemeSelectIndex, quickThemeSelectItems);
            if (newQuickThemeSelectIndex != quickThemeSelectIndex)
            {
                quickThemeSelectIndex = newQuickThemeSelectIndex;
                ThemeManager.Instance.currentThemeName = quickThemeSelectItems[quickThemeSelectIndex];
                UpdateThemeResources();
            }
        }

        if (GUILayout.Button("Update Theme Resources"))
        {
            UpdateThemeResources();
        }

        if (GUILayout.Button("Refresh Resources Folder"))
        {
            RefreshAssetsInResourcesFolder();
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

    private void RefreshAssetsInResourcesFolder()
    {
        // Update Unity's version of files in the Resources folder
        AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ImportRecursive);
    }

    private void UpdateQuickThemeSelect()
    {
        List<string> loadedThemeNames = ThemeManager.Instance.GetLoadedThemeNames();
        quickThemeSelectItems = loadedThemeNames.ToArray();
        Theme currentTheme = ThemeManager.Instance.GetCurrentTheme();
        if (currentTheme != null)
        {
            quickThemeSelectIndex = loadedThemeNames.IndexOf(currentTheme.Name);
        }
    }
}
