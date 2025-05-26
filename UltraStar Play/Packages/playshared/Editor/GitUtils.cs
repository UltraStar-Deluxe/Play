using System;
using System.IO;
using UnityEditor.Build;
using UnityEngine;

public static class GitUtils
{
    // Original implementation from https://www.stewmcc.com/post/git-commands-from-unity/
    public static string RunGitCommand(string arguments, bool throwOnFailure = true)
    {
        if (ProcessUtils.RunProcess("git", arguments,
                out string output,
                out string errorOutput,
                ELogEventLevel.Information,
                ELogEventLevel.Error))
        {
            // Check for fatal errors.
            if (output.IsNullOrEmpty() || output.Contains("fatal"))
            {
                string errorMessage = $"Command failed: git {arguments}\n{output}\n{errorOutput}\n";
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
        }
        else
        {
            Debug.LogError("Git is not set-up correctly, required to be on PATH, and to be a git project.");
            return "git-error";
        }

        return output;
    }

    public static string GetCurrentCommitShortHash()
    {
        if (PlatformUtils.IsWindows)
        {
            return DoGetCurrentCommitShortHash();
        }
        else
        {
            // TODO: running Git command to get the commit hash of the current branch fails with "fatal: unsafe repository ('/github/workspace' is owned by someone else)"
            string commitHash = GetMasterBranchCommitShortHash(out string errorMessage);
            if (!errorMessage.IsNullOrEmpty())
            {
                Debug.LogError(errorMessage);
                Debug.Log("Failed to get commit hash from master branch, falling back to getting commit hash from current branch using git command.");
                commitHash = DoGetCurrentCommitShortHash();
            }

            return commitHash;
        }
    }

    private static string DoGetCurrentCommitShortHash()
    {
        string commitShortHash = RunGitCommand("rev-parse --short --verify HEAD");

        // Clean up whitespace around hash. (seems to just be the way this command returns :/ )
        commitShortHash = string.Join("", commitShortHash.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        Debug.Log("Current commit short hash: " + commitShortHash);
        return commitShortHash;
    }

    private static string GetMasterBranchCommitShortHash(out string errorMessage)
    {
        string gitFolderPath = GetGitFolder();
        if (gitFolderPath.IsNullOrEmpty())
        {
            throw new BuildFailedException("No .git folder found");
        }

        // Get commit hash from .git/refs/heads/master
        string commitHashFile = gitFolderPath + "/refs/heads/master";
        if (!File.Exists(commitHashFile))
        {
            errorMessage = $"No file with commit hash found inside .git folder (tried file path: {commitHashFile})";
            return "";
        }

        Debug.Log($"commitHashFile: {commitHashFile}");
        string commitHash = File.ReadAllText(commitHashFile);
        Debug.Log($"commitHash: {commitHash}");
        string commitShortHash = commitHash[..8];
        Debug.Log($"commitShortHash: {commitShortHash}");
        errorMessage = "";
        return commitShortHash;
    }

    private static string GetGitFolder()
    {
        Debug.Log("Searching .git folder");
        string currentDirectory = Directory.GetCurrentDirectory();
        do
        {
            Debug.Log($"currentDirectory: {currentDirectory}");
            if (Directory.Exists(currentDirectory + "/.git"))
            {
                string gitFolder = currentDirectory + "/.git";
                Debug.Log($"Found .git folder: {gitFolder}");
                return gitFolder;
            }
            DirectoryInfo parentDirectory = Directory.GetParent(currentDirectory);
            currentDirectory = parentDirectory?.FullName;
        } while (!currentDirectory.IsNullOrEmpty() && Directory.Exists(currentDirectory));

        return null;
    }
}
