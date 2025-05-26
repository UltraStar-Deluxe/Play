using System.IO;
using UnityEngine;

public static class FileSystemWatcherFactory
{
    public static FileSystemWatcher CreateFileSystemWatcher(
        string folder,
        FileSystemWatcherConfig config,
        FileSystemEventHandler fileSystemEventHandler)
    {
        WatcherChangeTypes changeType = config.ChangeTypes;

        FileSystemWatcher fileSystemWatcher = new(folder, config.Filter);

        if (changeType.HasFlag(WatcherChangeTypes.Changed))
        {
            fileSystemWatcher.Changed += fileSystemEventHandler;
        }

        if (changeType.HasFlag(WatcherChangeTypes.Created))
        {
            fileSystemWatcher.Created += fileSystemEventHandler;
        }

        if (changeType.HasFlag(WatcherChangeTypes.Deleted))
        {
            fileSystemWatcher.Deleted += fileSystemEventHandler;
        }

        if (changeType.HasFlag(WatcherChangeTypes.Renamed))
        {
            fileSystemWatcher.Renamed += (sender, evt) => fileSystemEventHandler.Invoke(sender, evt);
        }

        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.CreationTime;

        fileSystemWatcher.IncludeSubdirectories = config.IncludeSubdirectories;

        fileSystemWatcher.EnableRaisingEvents = true;

        Debug.Log($"Created FileSystemWatcher: description '{config.Description}', folder '{folder}', filter: '{config.Filter}'");

        return fileSystemWatcher;
    }
}
