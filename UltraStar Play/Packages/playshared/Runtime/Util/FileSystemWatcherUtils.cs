using System.IO;
using UnityEngine;

public static class FileSystemWatcherUtils
{
    public static FileSystemWatcher CreateFileSystemWatcher(
        string folder,
        string filter,
        FileSystemEventHandler fileSystemEventHandler)
    {
        FileSystemWatcher fileSystemWatcher = new(folder, filter);

        fileSystemWatcher.Changed += fileSystemEventHandler;
        fileSystemWatcher.Created += fileSystemEventHandler;
        // Handle the other RenamedEventArgs also by using the fileSystemEventHandler
        fileSystemWatcher.Renamed += (sender, evt) => fileSystemEventHandler.Invoke(sender, evt);
        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.CreationTime;
        fileSystemWatcher.IncludeSubdirectories = false;
        fileSystemWatcher.EnableRaisingEvents = true;

        return fileSystemWatcher;
    }
}
