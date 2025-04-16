using System;
using System.IO;
using Nuke.Common.IO;

namespace DefaultNamespace;

public static class FileUtils
{
    public static void MoveFile(AbsolutePath source, AbsolutePath destination, FileMoveSettings settings)
    {
        DirectoryUtils.CreateDirectory(destination.Parent);
        File.Move(source, destination, settings.Overwrite);
    }

    public static void DeleteFile(AbsolutePath file)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    public class FileMoveSettings
    {
        public bool Overwrite { get; set; }
    }
}
