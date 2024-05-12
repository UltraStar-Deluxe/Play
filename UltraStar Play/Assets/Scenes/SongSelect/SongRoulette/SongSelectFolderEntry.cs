using System;
using System.IO;

public class SongSelectFolderEntry : SongSelectEntry
{
    public DirectoryInfo DirectoryInfo { get; private set; }

    public SongSelectFolderEntry(DirectoryInfo directoryInfo)
    {
        DirectoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
    }

    protected bool Equals(SongSelectFolderEntry other)
    {
        return Equals(DirectoryInfo, other.DirectoryInfo);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((SongSelectFolderEntry)obj);
    }

    public override int GetHashCode()
    {
        return DirectoryInfo.GetHashCode();
    }
}
