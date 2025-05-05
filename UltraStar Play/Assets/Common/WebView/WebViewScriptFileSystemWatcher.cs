using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class WebViewScriptFileSystemWatcher : AbstractSingletonBehaviour, INeedInjection
{
    public WebViewScriptFileSystemWatcher Instance => DontDestroyOnLoadManager.FindComponentOrThrow<WebViewScriptFileSystemWatcher>();

    [Inject]
    private WebViewManager webViewManager;

    private readonly List<IDisposable> disposables = new();

    protected override object GetInstance()
    {
        return Instance;
    }

#if UNITY_STANDALONE
    protected override void StartSingleton()
    {
        foreach (string folder in WebViewUtils.GetWebViewScriptsFolders())
        {
            if (!DirectoryUtils.Exists(folder))
            {
                continue;
            }
            Debug.Log($"Watching WebView script files in {folder}");
            disposables.Add(FileSystemWatcherFactory.CreateFileSystemWatcher(
                folder,
                new FileSystemWatcherConfig("WebViewScriptsFolderWatcher", "*.js"),
                OnWebViewScriptChanged));
        }
    }

    private void OnWebViewScriptChanged(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"WebView script file changed: {e.FullPath}");
        MainThreadDispatcher.Send(_ => webViewManager.ReloadScripts(), null);
    }
#endif

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
