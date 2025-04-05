using System;
using System.IO;
using Nuke.Common.IO;

namespace DefaultNamespace;

public static class DirectoryUtils
{
    public static void DeleteDirectory(AbsolutePath directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    public static void EnsureExistingDirectory(AbsolutePath directory)
    {
        Directory.CreateDirectory(directory);
    }

    public static void MoveDirectory(AbsolutePath source, AbsolutePath destination, DirectoryMoveSettings settings)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(source);
        }

        if (settings.Overwrite)
        {
            DeleteDirectory(destination);
        }

        Directory.Move(source, destination);
    }

    public class DirectoryMoveSettings
    {
        public bool Overwrite { get; set; }
    }
}
