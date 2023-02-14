using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Jint;
using Jint.Runtime.Debugger;
using UniInject;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class JintDemo : MonoBehaviour, INeedInjection
{
    private string ModsPath => $"{Application.persistentDataPath}/Mods";
    private string RuntimeScriptPath => $"{ModsPath}/out/js/index.js";

    private string[] jsCodeLines;
    private string jsCode;
    
    private void Start()
    {
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();
        
        if (!File.Exists(RuntimeScriptPath))
        {
            Debug.LogWarning("Cannot run script. File not found: " + RuntimeScriptPath);
            return;
        }

        GenerateTypeScriptDeclarations();
        
#if UNITY_EDITOR
        RunTypeScriptCompiler();
#endif
        
        RuntimeScriptRegistry runtimeScriptRegistry = new();

        Engine engine = new(config =>
            {
                config.Strict = true;
                config.Culture = CultureInfo.InvariantCulture;
                config.TimeZone = TimeZoneInfo.Utc;
                config.Debugger.Enabled = true;
                config.Debugger.InitialStepMode = StepMode.None;
                config.DebugMode();
                config.AllowClr();
            });
        engine.SetValue("log", new Action<object>(obj => Debug.Log($"js: {obj}")));
        engine.SetValue("persistentDataPath", Application.persistentDataPath);
        engine.SetValue("songMeta", new SongMeta());
        engine.SetValue(nameof(runtimeScriptRegistry), runtimeScriptRegistry);
        
        engine.DebugHandler.Step += JavaScriptEngineStep;
        engine.DebugHandler.Break += JavaScriptEngineBreak;

        jsCodeLines = File.ReadAllLines(RuntimeScriptPath);
        
        engine.DebugHandler.BreakPoints.Active = true;

        AddDebuggerStatementBreakpoints(engine, jsCodeLines);

        jsCode = jsCodeLines.JoinWith("\n");
        engine.Execute(jsCode);
        
        Debug.Log("Highscores: " +
                  runtimeScriptRegistry.HighscoreProviders
                      .Select(it => it.Name + " = " + it.GetScore())
                      .ToCsv());

        // if (!runtimeScriptRegistry.HighscoreProviders.IsNullOrEmpty())
        // {
        //     SongMeta songMeta = SongMetaManager.Instance.GetSongMetas()
        //         .FirstOrDefault(it => it.Title.Contains("Kryptonite"));
        //     Debug.Log("Note count: " + runtimeScriptRegistry.HighscoreProviders.FirstOrDefault().GetNoteCount(songMeta));
        // }
    }

    private void AddDebuggerStatementBreakpoints(Engine engine, string[] lines)
    {
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            int indexOfDebuggerStatement = line.IndexOf("debugger", StringComparison.InvariantCulture);
            if (indexOfDebuggerStatement >= 0)
            {
                engine.DebugHandler.BreakPoints.Set(new BreakPoint(lineIndex + 1, indexOfDebuggerStatement));
            }
        }
    }

    private void GenerateTypeScriptDeclarations()
    {
        TypeScriptDeclarationGenerator generator = new TypeScriptDeclarationGenerator();
        generator.AddInterfaceDeclaration(typeof(HighscoreProvider));
        generator.AddInterfaceDeclaration(typeof(SongMeta));
        generator.GenerateDeclarationsFile(ModsPath + "/out/typings/generated-types.d.ts");
    }

    private StepMode JavaScriptEngineStep(object sender, DebugInformation debugInformation)
    {
        if (debugInformation == null)
        {
            return StepMode.None;
        }

        string codeLine = jsCodeLines[debugInformation.Location.Start.Line - 1];
        Debug.Log($"JS Step - Current statement: {debugInformation}. Location: {debugInformation.Location}. Code line: {codeLine}");
        return StepMode.Into;
    }
    
    private StepMode JavaScriptEngineBreak(object sender, DebugInformation debugInformation)
    {
        if (debugInformation == null)
        {
            return StepMode.None;
        }

        string codeLine = jsCodeLines[debugInformation.Location.Start.Line - 1];
        Debug.Log($"JS Break - Current statement: {debugInformation}. Location: {debugInformation.Location}. Code line: {codeLine}");
        return StepMode.Into;
    }

#if UNITY_EDITOR
    private void RunTypeScriptCompiler()
    {
        string tscPath = ModsPath + "/node_modules/.bin/tsc";
        if (PlatformUtils.IsWindows
            || (Application.isEditor && PlatformUtils.IsEditorWindows))
        {
            tscPath += ".cmd";
        }
        string arguments = "--project tsconfig.json";
        ProcessStartInfo processInfo = new ProcessStartInfo(tscPath, arguments)
        {
            WorkingDirectory = ModsPath,
            CreateNoWindow = true, // We want no visible pop-ups
            UseShellExecute = false, // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true, // Allows us to read the output stream
            RedirectStandardError = true, // Allows us to read the error stream
        };

        // Start the Process
        Debug.Log("Starting tsc...");
        Process process = new() { StartInfo = processInfo };
        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"tsc failed. Output: {process.StandardOutput.ReadToEnd()}");
            return;
        }
        Debug.Log("tsc output: " + process.StandardOutput.ReadToEnd());
    }
#endif
}
