using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

[InitializeOnLoad]
public static class I18NFileSystemWatcher
{
    private static bool triggerUpdate;
    private static readonly FileSystemWatcher fileSystemWatcher;
    private static ConcurrentBag<string> changedFiles = new ConcurrentBag<string>();
    private static readonly string streamingAssetsPath;

    static I18NFileSystemWatcher()
    {
        streamingAssetsPath = Application.streamingAssetsPath;
        string path = streamingAssetsPath + "/" + I18NManager.I18NFolder;

        fileSystemWatcher = new FileSystemWatcher(path, "*.properties");
        fileSystemWatcher.Changed += OnFileChanged;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.EnableRaisingEvents = true;

        EditorApplication.update += OnEditorApplicationUpdate;

        Debug.Log("Watching translation files in: " + path);
    }

    private static void OnEditorApplicationUpdate()
    {
        // Note that this is called very often (100/sec). Thus, code in here should be fast.
        if (triggerUpdate)
        {
            triggerUpdate = false;

            I18NManager i18nManager = I18NManager.Instance;
            if (i18nManager != null)
            {
                List<string> quotedChangedFiles = changedFiles.Distinct().Select(it => $"'{it}'").ToList();
                string changedFilesCsv = string.Join(", ", quotedChangedFiles);
                changedFiles = new ConcurrentBag<string>();

                Debug.Log("Reloading translations because of changed files: " + changedFilesCsv);
                i18nManager.UpdateCurrentLanguageAndTranslations();
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
