using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneListEditorWindow : EditorWindow
{
    private List<string> scenePaths;

    private Vector2 scrollPos;
    private bool sortAlphabetically;

    private static readonly List<string> ignoredFolderNames = new List<string>
    {
        "Background Bokeh VFX",
        "CartoonVFX9X",
        "Confetti FX Pro",
        "Hovl Studio",
        "JMO Assets",
        "UnityStandaloneFileBrowser",
        "VLCUnity",
        "Vuplex",
    };

    private string fileNameRegEx = "";
    private string lastFilterText = "";

    [MenuItem("Window/Scene List")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(SceneListEditorWindow), false, "Scene List");
    }

    private void Awake()
    {
        UpdateScenePaths();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Refresh"))
        {
            scenePaths = FindScenePaths(sortAlphabetically);
        }
        fileNameRegEx = GUILayout.TextField(fileNameRegEx);
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

        if (lastFilterText != fileNameRegEx)
        {
            UpdateScenePaths();
        }

        lastFilterText = fileNameRegEx;
    }

    private void UpdateScenePaths()
    {
        scenePaths = FindScenePaths(sortAlphabetically);
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

    private bool IsIgnored(string path)
    {
        string fileName = Path.GetFileName(path);
        if (fileName.StartsWith("InitTestScene"))
        {
            return true;
        }

        string normalizedPath = path.Replace("\\", "/");
        return ignoredFolderNames.AnyMatch(ignoredFolderName => normalizedPath.Contains($"/{ignoredFolderName}/"));
    }

    private bool HasMatchingFileName(string path)
    {
        if (fileNameRegEx.IsNullOrEmpty())
        {
            return true;
        }

        string fileName = Path.GetFileNameWithoutExtension(path);
        return Regex.IsMatch(fileName, fileNameRegEx, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private List<string> FindScenePaths(bool sortAlphabetically)
    {
        string assetsFolder = "Assets";
        string[] paths = Directory.GetFiles(assetsFolder, "*.unity", SearchOption.AllDirectories);
        List<string> result = paths
            .Where(path => !IsIgnored(path))
            .Where(path => HasMatchingFileName(path))
            .ToList();
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
