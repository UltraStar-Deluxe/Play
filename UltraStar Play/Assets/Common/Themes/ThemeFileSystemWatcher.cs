using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ThemeFileSystemWatcher : AbstractSingletonBehaviour, INeedInjection
{
    public ThemeFileSystemWatcher Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ThemeFileSystemWatcher>();

    [Inject]
    private ThemeManager themeManager;

    private readonly List<IDisposable> disposables = new();

    protected override object GetInstance()
    {
        return Instance;
    }

#if UNITY_STANDALONE
    protected override void StartSingleton()
    {
        foreach (string themeFolder in ThemeFolderUtils.GetThemeFolders())
        {
            Debug.Log($"Watching theme files in '{themeFolder}'");
            disposables.Add(FileSystemWatcherUtils.CreateFileSystemWatcher(
                themeFolder,
                "*.json",
                OnThemeFileChanged));
        }
    }

    private void OnThemeFileChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"Theme file changed: {e.FullPath}");
        MainThreadDispatcher.Send(_ => themeManager.ReloadThemes(), null);
    }
#endif

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
