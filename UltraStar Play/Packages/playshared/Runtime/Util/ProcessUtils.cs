using System;
using System.Diagnostics;
using Serilog.Events;

public static class ProcessUtils
{
    public static bool RunProcess(
        string executable,
        string arguments,
        out string output,
        out string errorOutput,
        LogEventLevel outputLogLevel = LogEventLevel.Debug,
        LogEventLevel errorOutputLogLevel = LogEventLevel.Debug)
    {
        Log.WithLevel(outputLogLevel, () => $"Executing process '{executable} {arguments}'");

        // Set up our processInfo to run the git command and log to output and errorOutput.
        ProcessStartInfo processInfo = new(executable, arguments)
        {
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true // Allows us to read the error stream
        };

        // Set up the Process
        using Process process = new() { StartInfo = processInfo };

        try
        {
            // Try to start it, catching any exceptions if it fails
            process.Start();
        }
        catch (Exception e)
        {
            // For now just assume its failed cause it can't find git.
            UnityEngine.Debug.LogException(e);
            UnityEngine.Debug.LogError($"Failed to execute process '{executable} {arguments}': {e.Message}");
            output = "";
            errorOutput = "";
            return false;
        }

        process.WaitForExit(); // Make sure we wait till the process has fully finished.

        // Read the results back from the process so we can get the output and check for errors
        string processOutput = process.StandardOutput.ReadToEnd();
        string processErrorOutput = process.StandardError.ReadToEnd();
        output = processOutput;
        errorOutput = processErrorOutput;

        if (!processOutput.IsNullOrEmpty())
        {
            Log.WithLevel(outputLogLevel, () => $"Output of '{executable} {arguments}':\n{processOutput}");
        }

        if (!processErrorOutput.IsNullOrEmpty())
        {
            Log.WithLevel(errorOutputLogLevel, () => $"Error output of '{executable} {arguments}':\n{processErrorOutput}");
        }

        if (process.ExitCode != 0)
        {
            return false;
        }

        return true;
    }
}
