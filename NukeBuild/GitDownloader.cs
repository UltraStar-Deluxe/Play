using System;
using System.Collections.Generic;
using System.IO;
using Nuke.Common.IO;
using static Nuke.Common.Tools.Git.GitTasks;

namespace DefaultNamespace;

public class GitDownloader
{
    public string RemoteUrl { get; set; }
    public string CommitHash { get; set; }
    public string Branch { get; set; } = "main";
    public AbsolutePath TargetDir { get; set; }
    public uint Depth { get; set; }
    public List<string> SparseCheckoutPatterns { get; set; } = new();
    public Dictionary<string, string> MovePostprocess { get; set; } = new();
    public List<string> DeletePostprocess { get; set; } = new();

    public void Download()
    {
        string oldDir = Directory.GetCurrentDirectory();
        try
        {
            PrepareTargetDirectory();
            CloneRepository();
            MoveFiles();
            DeleteFiles();

            Console.WriteLine($"Done downloading dependency from Git: RemoteUrl='{RemoteUrl}', CommitHash='{CommitHash}'");
        }
        finally
        {
            Console.WriteLine($"Done downloading dependency from Git: RemoteUrl='{RemoteUrl}', CommitHash='{CommitHash}'");
        }
    }

    private void PrepareTargetDirectory()
    {
        Console.WriteLine($"Removing old folder: TargetDir='{TargetDir}'");
        DirectoryUtils.DeleteDirectory(TargetDir);

        Console.WriteLine($"Creating new folder: TargetDir='{TargetDir}'");
        DirectoryUtils.CreateDirectory(TargetDir);
    }

    private void CloneRepository()
    {
        Console.WriteLine($"Cloning from remote: RemoteUrl='{RemoteUrl}', CommitHash='{{CommitHash}}', Depth='{Depth}'");

        Git($"init", workingDirectory: TargetDir);
        Git($"remote add origin {RemoteUrl}", workingDirectory: TargetDir);

        ConfigureSparseCheckout();

        string depthArgument = Depth > 0 ? $"--depth={Depth}" : "";
        Git($"pull {depthArgument} origin {Branch}", workingDirectory: TargetDir);
        Git($"checkout {CommitHash}", workingDirectory: TargetDir);
    }

    private void ConfigureSparseCheckout()
    {
        if (SparseCheckoutPatterns.Count > 0)
        {
            Git("config core.sparsecheckout true", workingDirectory: TargetDir);
            AbsolutePath sparseCheckoutFile = TargetDir / ".git/info/sparse-checkout";
            Directory.CreateDirectory(Path.GetDirectoryName(sparseCheckoutFile));
            File.WriteAllLines(sparseCheckoutFile, SparseCheckoutPatterns);
        }
    }

    private void MoveFiles()
    {
        if (MovePostprocess.Count > 0)
        {
            Console.WriteLine("Moving downloaded files to correct position for this project...");
            foreach ((string source, string destination) in MovePostprocess)
            {
                MoveFileOrDirectory(source, destination);
            }
        }
    }

    private void MoveFileOrDirectory(string source, string destination)
    {
        Console.WriteLine($"Moving '{source}' to '{destination}'");
        if (source.EndsWith("*"))
        {
            MoveDirectoryContents(source.Substring(0, source.Length - 1), destination);
            return;
        }

        if (File.Exists(source))
        {
            FileUtils.MoveFile(TargetDir / source, TargetDir / destination, new FileUtils.FileMoveSettings { Overwrite = true });
        }
        else if (Directory.Exists(source))
        {
            DirectoryUtils.MoveDirectory(TargetDir / source, TargetDir / destination, new DirectoryUtils.DirectoryMoveSettings { Overwrite = true });
        }
        else
        {
            throw new FileNotFoundException(source);
        }
    }

    private void MoveDirectoryContents(string sourceDir, string destinationDir)
    {
        AbsolutePath sourcePath = TargetDir / sourceDir;
        AbsolutePath destPath = TargetDir / destinationDir;

        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDir}' not found");
        }

        DirectoryUtils.CreateDirectory(destPath);

        // Move all files
        foreach (string file in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            FileUtils.MoveFile(file, TargetDir / destFile, new FileUtils.FileMoveSettings { Overwrite = true });
        }

        // Move all subdirectories
        foreach (string dir in Directory.GetDirectories(sourcePath))
        {
            string dirName = Path.GetFileName(dir);
            string destDir = Path.Combine(destinationDir, dirName);
            DirectoryUtils.MoveDirectory(dir, TargetDir / destDir, new DirectoryUtils.DirectoryMoveSettings { Overwrite = true });
        }
    }

    private void DeleteFiles()
    {
        if (DeletePostprocess.Count > 0)
        {
            foreach (string path in DeletePostprocess)
            {
                DeleteFileOrDirectory(path);
            }
        }
    }

    private void DeleteFileOrDirectory(string path)
    {
        Console.WriteLine($"Deleting '{path}'");
        AbsolutePath absolutePath = TargetDir / path;

        if (File.Exists(absolutePath))
        {
            FileUtils.DeleteFile(absolutePath);
        }
        else if (Directory.Exists(absolutePath))
        {
            DirectoryUtils.DeleteDirectory(absolutePath);
        }
        else
        {
            throw new FileNotFoundException(absolutePath);
        }
    }
}
