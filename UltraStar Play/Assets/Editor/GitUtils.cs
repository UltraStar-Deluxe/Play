using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class GitUtils
{
    // Original implementation from https://www.stewmcc.com/post/git-commands-from-unity/
    public static string RunGitCommand(string gitCommand)
    {
        // Strings that will catch the output from our process.
        string output = "no-git";
        string errorOutput = "no-git";

        // Set up our processInfo to run the git command and log to output and errorOutput.
        ProcessStartInfo processInfo = new ProcessStartInfo("git", @gitCommand)
        {
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true // Allows us to read the error stream
        };

        // Set up the Process
        Process process = new Process {StartInfo = processInfo};

        try
        {
            process.Start(); // Try to start it, catching any exceptions if it fails
        }
        catch (Exception e)
        {
            // For now just assume its failed cause it can't find git.
            Debug.LogError("Git is not set-up correctly, required to be on PATH, and to be a git project.");
            throw e;
        }

        // Read the results back from the process so we can get the output and check for errors
        output = process.StandardOutput.ReadToEnd();
        errorOutput = process.StandardError.ReadToEnd();

        process.WaitForExit(); // Make sure we wait till the process has fully finished.
        process.Close(); // Close the process ensuring it frees it resources.

        // Check for failure due to no git setup in the project itself or other fatal errors from git.
        if (output.Contains("fatal") || output == "no-git" || output == "")
        {
            throw new Exception("Command: git " + @gitCommand + " Failed\n" + output + errorOutput);
        }

        // Log any errors.
        if (errorOutput != "")
        {
            Debug.LogError("Git Error: " + errorOutput);
        }

        return output; // Return the output from git.
    }
    
    public static string GetCurrentCommitShortHash() {
        string result = RunGitCommand("rev-parse --short --verify HEAD");
        // Clean up whitespace around hash. (seems to just be the way this command returns :/ )
        result = string.Join("", result.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        Debug.Log("Current Commit: " + result);
        return result;
    }
}
