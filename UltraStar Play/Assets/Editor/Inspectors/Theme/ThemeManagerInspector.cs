using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomEditor(typeof(ThemeManager))]
public class ThemeManagerInspector : EditorBase
{
    int quickThemeSelectIndex;
    string[] quickThemeSelectItems;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (quickThemeSelectItems.IsNullOrEmpty()
            || (ThemeManager.CurrentTheme != null
                && ThemeManager.CurrentTheme.Name != ThemeManager.Instance.currentThemeName))
        {
            UpdateQuickThemeSelect();
        }

        if (!quickThemeSelectItems.IsNullOrEmpty())
        {
            int newQuickThemeSelectIndex = EditorGUILayout.Popup("Quick Theme Select", quickThemeSelectIndex, quickThemeSelectItems);
            if (newQuickThemeSelectIndex != quickThemeSelectIndex)
            {
                quickThemeSelectIndex = newQuickThemeSelectIndex;

                Undo.RecordObject(ThemeManager.Instance, $"Set currentThemeName");
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
        if (ThemeManager.CurrentTheme == null)
        {
            Debug.LogError("CurrentTheme is null");
            return;
        }

        // Add Undo entry for Themeable changes
        Themeable[] themeables = FindObjectsOfType<Themeable>();
        foreach (Themeable themeable in themeables)
        {
            RegisterUndo(themeable);
        }

        // Update the themes
        ThemeManager.Instance.UpdateThemeResources();
        UpdateQuickThemeSelect();
    }

    private void RegisterUndo(Themeable themeable)
    {
        themeable.GetAffectedObjects().Where(it => it != null).ForEach(affectedObject =>
        {
            Undo.RecordObject(affectedObject, $"Apply theme '{ThemeManager.CurrentTheme.Name}'");
            // For some reason, Undo.RecordObject is not enough. Thus, also mark the affected object as dirty.
            EditorUtility.SetDirty(affectedObject);
            if (affectedObject is MonoBehaviour so)
            {
                EditorUtility.SetDirty(so.gameObject);
            }
        });
    }

    private void UpdateQuickThemeSelect()
    {
        List<string> loadedThemeNames = ThemeManager.GetThemeNames();
        quickThemeSelectItems = loadedThemeNames.ToArray();
        if (ThemeManager.CurrentTheme != null)
        {
            quickThemeSelectIndex = loadedThemeNames.IndexOf(ThemeManager.CurrentTheme.Name);
        }
    }
}
