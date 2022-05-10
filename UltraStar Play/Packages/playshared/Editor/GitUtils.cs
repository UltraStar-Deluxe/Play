using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

public static class GitUtils
{
    // Original implementation from https://www.stewmcc.com/post/git-commands-from-unity/
    public static string RunGitCommand(string gitCommand, bool throwOnFailure = true)
    {
        // Set up our processInfo to run the git command and log to output and errorOutput.
        ProcessStartInfo processInfo = new("git", gitCommand)
        {
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true // Allows us to read the error stream
        };

        // Set up the Process
        Process process = new() { StartInfo = processInfo };

        try
        {
            // Try to start it, catching any exceptions if it fails
            process.Start();
        }
        catch (Exception e)
        {
            // For now just assume its failed cause it can't find git.
            Debug.LogException(e);
            Debug.LogError("Git is not set-up correctly, required to be on PATH, and to be a git project.");
            return "git-error";
        }

        // Read the results back from the process so we can get the output and check for errors
        string output = process.StandardOutput.ReadToEnd();
        string errorOutput = process.StandardError.ReadToEnd();

        process.WaitForExit(); // Make sure we wait till the process has fully finished.
        process.Close(); // Close the process ensuring it frees it resources.

        // Check for fatal errors.
        if (output.IsNullOrEmpty() || output.Contains("fatal"))
        {
            string errorMessage = $"Command failed: git {gitCommand}\n{output}\n{errorOutput}\n";
            if (throwOnFailure)
            {
                throw new BuildFailedException(errorMessage);
            }

            Debug.LogError(errorMessage);
            return "";
        }

        // Log any errors.
        if (!errorOutput.IsNullOrEmpty())
        {
            Debug.LogError("Git Error: " + errorOutput);
        }

        return output;
    }

    public static string GetCurrentCommitShortHash()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string firstGitFolderPath = null;
        do
        {
            ListDirectoryContent(currentDirectory);
            DirectoryInfo parentDirectory = Directory.GetParent(currentDirectory);
            currentDirectory = parentDirectory?.FullName;
            if (firstGitFolderPath.IsNullOrEmpty()
                && Directory.Exists(currentDirectory + "/.git"))
            {
                firstGitFolderPath = currentDirectory + "/.git";
            }
        } while (!currentDirectory.IsNullOrEmpty() && Directory.Exists(currentDirectory));

        // Get commit hash from .git/refs/heads/master
        string commitHashFile = firstGitFolderPath + "/refs/heads/master";
        Debug.Log($"commitHashFile: {commitHashFile}");
        string commitHash = File.ReadAllText(commitHashFile);
        Debug.Log($"commitHash: {commitHash}");
        string commitShortHash = commitHash[..8];
        Debug.Log($"commitShortHash: {commitShortHash}");

        // Alternative: run git command
        // string result = RunGitCommand("rev-parse --short --verify HEAD");

        // Clean up whitespace around hash. (seems to just be the way this command returns :/ )
        // result = string.Join("", result.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        // Debug.Log("Current commit short hash: " + result);
        return commitShortHash;
    }

    private static void ListDirectoryContent(string path)
    {
        string[] allFolders = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
        allFolders.ForEach(folder => Debug.Log(folder));
    }
}
