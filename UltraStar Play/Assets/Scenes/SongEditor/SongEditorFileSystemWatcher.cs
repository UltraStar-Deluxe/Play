using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorFileSystemWatcher : MonoBehaviour, INeedInjection
{
#if UNITY_STANDALONE

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private UiManager uiManager;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    private readonly HashSet<string> changedMediaFiles = new HashSet<string>();

    private void Start()
    {
        string path = songMeta.Directory;
        if (!Directory.Exists(path))
        {
            return;
        }

        List<string> filters = new List<string> { "*.ogg", "*.mp3", "*.wav",
            "*.jpg", "*.png",
            "*.vp8", "*.webm", "*.mp4", "*.mpeg", "*.mpg", "*.avi", "*.wmv"  };
        filters.ForEach(filter =>
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path, filter);
            disposables.Add(fileSystemWatcher);

            fileSystemWatcher.Changed += OnFileChanged;
            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.EnableRaisingEvents = true;
        });

        Debug.Log("Watching song media files in: " + path);
    }

    private void Update()
    {
        if (changedMediaFiles.IsNullOrEmpty())
        {
            return;
        }

        string changedFileNamesCsv = string.Join(",", changedMediaFiles.Select(path => Path.GetFileName(path)));
        changedMediaFiles.Clear();

        // TODO: Reload media files without restart of SongEditorScene
        Debug.Log($"Restarting SongEditorScene because of changed media files: {changedFileNamesCsv}");
        uiManager.CreateNotificationVisualElement($"Restarting SongEditorScene because of changed media files:\n{changedFileNamesCsv}");

        // Clear cache to see changed media files
        ImageManager.ClearCache();
        AudioManager.ClearCache();
        songEditorSceneControl.RestartSongEditorScene();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Do not any Unity API because we are not in the main thread.
        changedMediaFiles.Add(e.FullPath);
    }

    private void OnDestroy()
    {
        disposables.ForEach(disposable => disposable.Dispose());
    }

#endif
}
