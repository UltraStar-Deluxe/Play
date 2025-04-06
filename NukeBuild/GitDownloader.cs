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

            Console.WriteLine($"Done downloading dependency from GitHub: RemoteUrl='{RemoteUrl}', CommitHash='{CommitHash}'");
        }
        finally
        {
            Directory.SetCurrentDirectory(oldDir);
        }
    }

    private void PrepareTargetDirectory()
    {
        Console.WriteLine($"Removing old folder: TargetDir='{TargetDir}'");
        Directory.SetCurrentDirectory(TargetDir.Parent.ToString());
        DirectoryUtils.DeleteDirectory(TargetDir);
        DirectoryUtils.EnsureExistingDirectory(TargetDir);
        Directory.SetCurrentDirectory(TargetDir.ToString());
    }

    private void CloneRepository()
    {
        Console.WriteLine($"Cloning from remote: RemoteUrl='{RemoteUrl}', CommitHash='{{CommitHash}}', Depth='{Depth}'");
        Git("init");
        Git($"remote add origin {RemoteUrl}");

        ConfigureSparseCheckout();

        string depthArgument = Depth > 0 ? $"--depth {Depth}" : "";
        Git($"pull {depthArgument} origin main");
        Git($"checkout {CommitHash}");
    }

    private void ConfigureSparseCheckout()
    {
        if (SparseCheckoutPatterns.Count > 0)
        {
            Git("config core.sparsecheckout true");
            string sparseCheckoutFile = ".git/info/sparse-checkout";
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
        if (File.Exists(path))
        {
            FileUtils.DeleteFile(TargetDir / path);
        }
        else if (Directory.Exists(path))
        {
            DirectoryUtils.DeleteDirectory(TargetDir / path);
        }
        else
        {
            throw new FileNotFoundException(path);
        }
    }
}
