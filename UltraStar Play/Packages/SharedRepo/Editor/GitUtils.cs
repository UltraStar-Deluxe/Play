using System;
using System.Diagnostics;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

public static class GitUtils
{
    // Original implementation from https://www.stewmcc.com/post/git-commands-from-unity/
    public static string RunGitCommand(string gitCommand)
    {
        // Set up our processInfo to run the git command and log to output and errorOutput.
        ProcessStartInfo processInfo = new("git", @gitCommand)
        {
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true // Allows us to read the error stream
        };

        // Set up the Process
        Process process = new() {StartInfo = processInfo};

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

        // Check for failure due to no git setup in the project itself or other fatal errors from git.
        if (output.IsNullOrEmpty() || output.Contains("fatal"))
        {
            throw new BuildFailedException("Command: git " + @gitCommand + " Failed\n" + output + errorOutput);
        }

        // Log any errors.
        if (errorOutput != "")
        {
            Debug.LogError("Git Error: " + errorOutput);
        }

        return output;
    }

    public static string GetCurrentCommitShortHash()
    {
        string result = RunGitCommand("rev-parse --short --verify HEAD");
        // Clean up whitespace around hash. (seems to just be the way this command returns :/ )
        result = string.Join("", result.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        Debug.Log("Current commit short hash: " + result);
        return result;
    }
}
