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
            // To delete .git folder, some files need to be set from ReadOnly to Normal first
            foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(directory, true);
        }
    }

    public static void CreateDirectory(AbsolutePath directory)
    {
        Directory.CreateDirectory(directory);
    }

    public static void MoveFiles(AbsolutePath sourceDir, AbsolutePath destinationDir, SearchOption searchOption, params string[] fileNamePatterns)
    {
        foreach (var pattern in fileNamePatterns)
        {
            // Get files matching the current pattern
            foreach (var file in Directory.GetFiles(sourceDir, pattern, searchOption))
            {
                Console.WriteLine($"Moving file: Source='{file}', Target='{destinationDir}'");
                FileUtils.MoveFile(file, $"{destinationDir}/{Path.GetFileName(file)}", new FileUtils.FileMoveSettings() {Overwrite = true});
            }
        }
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
