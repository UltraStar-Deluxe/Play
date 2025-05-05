using System.IO;

public class FileSystemWatcherConfig
{
    public string Description { get; }
    public string Filter { get; }
    public WatcherChangeTypes ChangeTypes { get; set; } = WatcherChangeTypes.Created | WatcherChangeTypes.Changed | WatcherChangeTypes.Renamed;
    public bool IncludeSubdirectories { get; set; }

    public FileSystemWatcherConfig(string description, string filter)
    {
        Description = description;
        Filter = filter;
    }
}
