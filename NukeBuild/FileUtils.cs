using System;
using System.IO;
using Nuke.Common.IO;

namespace DefaultNamespace;

public static class FileUtils
{
    public static void MoveFile(AbsolutePath source, AbsolutePath destination, FileMoveSettings settings)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException(source);
        }

        if (settings.Overwrite)
        {
            DeleteFile(destination);
        }

        File.Move(source, destination);
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
