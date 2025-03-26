using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;

public class SongFoldersFileSystemWatcherControl : AbstractSingletonBehaviour, INeedInjection
{
    public static SongFoldersFileSystemWatcherControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongFoldersFileSystemWatcherControl>();

    [Inject]
    private Settings settings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongIssueManager songIssueManager;

    private List<string> watchedSongFolders;

    private readonly List<FileSystemWatcher> fileSystemWatchers = new();
    private readonly ConcurrentBag<string> ignoredFilePaths = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    void Start()
    {
        songMetaManager.BeforeSongMetaSavedEventStream.Subscribe(songMeta =>
        {
            if (songMeta.FileInfo != null)
            {
                // Do not reload this SongMeta when the file changes.
                ignoredFilePaths.Add(songMeta.FileInfo.FullName);
            }
        });

        AwaitableUtils.ExecuteRepeatedlyInSecondsAsync(gameObject, 1, CheckSongFoldersChanged);
    }

    private void CheckSongFoldersChanged()
    {
        List<string> enabledSongFolders = SettingsUtils.GetEnabledSongFolders(settings);
        if (watchedSongFolders != null
            && watchedSongFolders.SequenceEqual(enabledSongFolders))
        {
            return;
        }
        watchedSongFolders = enabledSongFolders;

        UpdateFileSystemWatchers(enabledSongFolders);
    }

    private void UpdateFileSystemWatchers(List<string> songFolders)
    {
        Debug.Log($"Updating FileSystemWatchers for song folders: {songFolders.JoinWith(", ", "[", "]")}");
        Dispose();
        songFolders
            .Where(songFolder => DirectoryUtils.Exists(songFolder))
            .ForEach(songFolder => fileSystemWatchers.Add(CreateFileSystemWatcher(songFolder)));
    }

    private FileSystemWatcher CreateFileSystemWatcher(string songFolder)
    {
        return FileSystemWatcherFactory.CreateFileSystemWatcher(songFolder,
            new FileSystemWatcherConfig("SongFolderWatcher", "*.txt")
            {
                ChangeTypes = WatcherChangeTypes.Changed | WatcherChangeTypes.Created | WatcherChangeTypes.Deleted,
                IncludeSubdirectories = true
            },
            (sender, evt) => OnTxtFileChanged(evt.FullPath, evt.ChangeType));
    }

    private void OnTxtFileChanged(string filePath, WatcherChangeTypes changeType)
    {
        if (!songMetaManager.IsSongScanFinished)
        {
            return;
        }

        if (ignoredFilePaths.Contains(filePath))
        {
            Log.Debug(() => $"Ignoring change to song txt file change because it has been saved by the game: path '{filePath}'");
            return;
        }

        SongMeta songMeta = songMetaManager.GetSongMetaByTxtFilePath(filePath);
        if (songMeta == null)
        {
            if (changeType.HasFlag(WatcherChangeTypes.Created)
                || (changeType.HasFlag(WatcherChangeTypes.Changed) && FileUtils.Exists(filePath)))
            {
                Log.Debug(() => $"Adding song because txt file was created: '{filePath}'");
                UltraStarSongParserResult result = UltraStarSongParser.ParseFile(filePath);
                songMetaManager.AddSongMeta(result.SongMeta);
                songIssueManager.AddSongIssues(result.SongIssues);
                return;
            }
        }
        else
        {
            if (changeType.HasFlag(WatcherChangeTypes.Deleted)
                || (changeType.HasFlag(WatcherChangeTypes.Changed) && !FileUtils.Exists(filePath)))
            {
                Log.Debug(() => $"Removing song because txt file was deleted: '{filePath}'");
                songMetaManager.RemoveSong(songMeta);
                return;
            }

            if (changeType.HasFlag(WatcherChangeTypes.Changed))
            {
                Log.Debug(() => $"Reloading song because txt file was changed: '{filePath}'");
                songMetaManager.ReloadSong(songMeta);
                return;
            }
        }
    }

    private void Dispose()
    {
        fileSystemWatchers.ForEach(it => it.Dispose());
        fileSystemWatchers.Clear();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
