using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;
using System;
using System.Globalization;

public class SceneListEditorWindow : EditorWindow
{
    private List<string> scenePaths;

    private Vector2 scrollPos;
    private bool sortAlphabetically;

    [MenuItem("Window/Scene List")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(SceneListEditorWindow), false, "Scene List");
    }

    void OnGUI()
    {
        if (scenePaths == null || GUILayout.Button("Refresh"))
        {
            scenePaths = FindScenePaths(sortAlphabetically);
        }
        sortAlphabetically = GUILayout.Toggle(sortAlphabetically, "Sort alphabetically");
        GUILayout.Label("---");

        if (scenePaths.Count == 0)
        {
            GUILayout.Label("No scenes found");
        }
        else
        {
            DrawSceneButtons();
        }
    }

    private void DrawSceneButtons()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (string scenePath in scenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (GUILayout.Button(sceneName))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private List<string> FindScenePaths(bool sortAlphabetically)
    {
        string assetsFolder = "Assets";
        string[] files = Directory.GetFiles(assetsFolder, "*.unity", SearchOption.AllDirectories);
        List<string> result = files.ToList();
        if (sortAlphabetically)
        {
            result.Sort(new PathNameComparer());
        }
        return result;
    }

    private class PathNameComparer : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            string s1Name = Path.GetFileNameWithoutExtension(s1);
            string s2Name = Path.GetFileNameWithoutExtension(s2);
            return string.Compare(s1Name, s2Name, true, CultureInfo.InvariantCulture);
        }
    }
}
