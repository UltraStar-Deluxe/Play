using System;
using System.IO;

public class SongSelectFolderEntry : SongSelectEntry
{
    public DirectoryInfo DirectoryInfo { get; private set; }

    public SongSelectFolderEntry(DirectoryInfo directoryInfo)
    {
        DirectoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
    }
}
