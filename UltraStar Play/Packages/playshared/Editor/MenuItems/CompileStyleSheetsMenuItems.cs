using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

public static class CompileStyleSheetsMenuItems
{
    [MenuItem("Generate/Compile SCSS Style Sheets (Sass)")]
    public static void CompileScssStyleSheets()
    {
        List<string> scssFiles = FileScanner.GetFiles("Packages/playshared/Runtime",
            new FileScannerConfig("*.scss") {Recursive = true});

        foreach (string scssFile in scssFiles)
        {
            string processOutput = CompileScssFile(scssFile);
            Debug.Log($"Compiled scss file {scssFile}. Process output: {processOutput}");
        }
    }

    private static string CompileScssFile(string scssFile)
    {
        string ussFile = Path.GetDirectoryName(scssFile) + "/" + Path.GetFileNameWithoutExtension(scssFile) + ".uss";
        string absoluteScssFile = new FileInfo(scssFile).FullName;
        string absoluteUssFile = new FileInfo(ussFile).FullName;

        string sassArguments = $"\"{absoluteScssFile}\" \"{absoluteUssFile}\"";
        ProcessStartInfo processInfo = new(GetSassCommand(), sassArguments)
        {
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true, // Allows us to read the error stream
        };

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
            Debug.LogError("Failed to compile style sheets.");
        }

        return process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
    }

    private static string GetSassCommand()
    {
#if UNITY_STANDALONE_WIN
        return "sass.cmd";
#else
        return "sass";
#endif
    }
}
