using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

[InitializeOnLoad]
public static class ThemeFileSystemWatcher
{
    private static bool triggerUpdate;
    private static List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();
    private static ConcurrentBag<string> changedFiles = new ConcurrentBag<string>();
    private static readonly string streamingAssetsPath;

    static ThemeFileSystemWatcher()
    {
        streamingAssetsPath = Application.streamingAssetsPath;
        string path = Application.streamingAssetsPath + "/" + ThemeManager.ThemesFolderName;

        List<string> filters = new List<string> { "*.properties", "*.png", "*.wav", "*.ogg" };
        foreach (string filter in filters)
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path, filter);
            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.EnableRaisingEvents = true;

            fileSystemWatchers.Add(fileSystemWatcher);
        }

        EditorApplication.update += OnEditorApplicationUpdate;

        Debug.Log("Watching theme files in: " + path);
    }

    private static void OnEditorApplicationUpdate()
    {
        // Note that this is called very often (100/sec). Thus, code in here should be fast.
        if (triggerUpdate)
        {
            triggerUpdate = false;
            ThemeManager themeManager = ThemeManager.Instance;
            if (themeManager != null)
            {
                List<string> quotedChangedFiles = changedFiles.Distinct().Select(it => $"'{it}'").ToList();
                string changedFilesCsv = string.Join(", ", quotedChangedFiles);
                changedFiles = new ConcurrentBag<string>();

                Debug.Log("Reloading themes because of changed files: " + changedFilesCsv);
                themeManager.ReloadThemes();
                themeManager.UpdateThemeResources();
            }
        }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Never call any Unity API because we are not in the main thread
        string relativePath = e.FullPath.Substring(streamingAssetsPath.Length + 1); // +1 for the last slash
        changedFiles.Add(relativePath);
        triggerUpdate = true;
    }
}
