using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if UNITY_EDITOR_OSX
[InitializeOnLoad]
#endif
public class MacOSQuarantineRemover : EditorWindow
#if UNITY_EDITOR_OSX
    , IPreprocessBuildWithReport
#endif
{
    private const string PLUGINS_PATH = "Assets/VLCUnity/Plugins/MacOS/ARM64";

    private bool hasQuarantine = false;
    private bool isChecking = false;
    private string command = "";
    private bool showCopyCommand = false;

#if UNITY_EDITOR_OSX
    public int callbackOrder => 0;
#endif

    #if UNITY_EDITOR_OSX
    static MacOSQuarantineRemover()
    {
        EditorApplication.delayCall += CheckQuarantineOnLoad;
    }

    private static void CheckQuarantineOnLoad()
    {
        if (CheckQuarantineStatusSilent())
        {
            UnityEngine.Debug.LogWarning(
                "[VLC Unity] macOS is blocking the VLC plugins. Go to Tools > VLC Unity > macOS Plugin Setup to fix this."
            );
        }
    }

    [MenuItem("Tools/VLC Unity/macOS Plugin Setup", false, 100)]
    #endif
    public static void ShowWindow()
    {
        #if UNITY_EDITOR_OSX
        var window = GetWindow<MacOSQuarantineRemover>("VLC Unity - macOS Setup");
        window.minSize = new Vector2(450, 280);
        window.Show();
        window.CheckQuarantineStatus();
        #endif
    }

#if UNITY_EDITOR_OSX
    public void OnPreprocessBuild(BuildReport report)
    {
        if (CheckQuarantineStatusSilent())
        {
            UnityEngine.Debug.LogWarning(
                "[VLC Unity] macOS is blocking the VLC plugins. " +
                "The built application may not run properly. " +
                "Go to Tools > VLC Unity > macOS Plugin Setup to fix this before building."
            );
        }
    }
#endif

    private void OnGUI()
    {
        GUILayout.Space(10);

        // Why is this needed explanation
        EditorGUILayout.HelpBox(
            "Why is this needed?\n\n" +
            "VLC Unity plugins are currently unsigned. macOS blocks unsigned code from running in the Unity Editor. " +
            "This tool authorizes the plugins for Editor testing.\n\n" +
            "Standalone builds are not affected and work normally.\n\n" +
            "Alternative: If you have an Apple Developer certificate, you can sign the binaries yourself to permanently resolve this.",
            MessageType.Info
        );

        GUILayout.Space(15);

        // State A: Checking
        if (isChecking)
        {
            EditorGUILayout.LabelField("Checking plugin status...", EditorStyles.boldLabel);
            return;
        }

        // State B: Blocked (quarantine detected)
        if (hasQuarantine)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(20));
            EditorGUILayout.LabelField("Plugins are blocked by macOS", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Authorize Plugins", GUILayout.Height(40)))
            {
                RemoveQuarantine();
            }

            EditorGUILayout.LabelField("(requires password)", EditorStyles.miniLabel);

            GUILayout.Space(15);

            showCopyCommand = EditorGUILayout.Foldout(showCopyCommand, "Advanced: Copy terminal command", true);
            if (showCopyCommand)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("Copy Command to Clipboard", GUILayout.Height(25)))
                {
                    EditorGUIUtility.systemCopyBuffer = command;
                    UnityEngine.Debug.Log($"[VLC Unity] Command copied to clipboard: {command}");
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        // State C: Ready (no quarantine)
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(EditorGUIUtility.IconContent("console.infoicon"), GUILayout.Width(20));
            EditorGUILayout.LabelField("Plugins are authorized and ready to use in the Editor", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Re-check Status", GUILayout.Height(25)))
            {
                CheckQuarantineStatus();
            }

            GUILayout.Space(15);

            showCopyCommand = EditorGUILayout.Foldout(showCopyCommand, "Advanced", true);
            if (showCopyCommand)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("Unauthorize Plugins", GUILayout.Height(25)))
                {
                    AddQuarantine();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                EditorGUILayout.LabelField("Restore macOS block for testing purposes", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private static bool CheckQuarantineStatusSilent()
    {
        #if UNITY_EDITOR_OSX
        if (!Directory.Exists(PLUGINS_PATH))
        {
            return false;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xattr",
                Arguments = $"-r \"{PLUGINS_PATH}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Contains("com.apple.quarantine");
        }
        catch
        {
            return false;
        }
        #else
        return false;
        #endif
    }

    private void CheckQuarantineStatus()
    {
        #if UNITY_EDITOR_OSX
        if (!Directory.Exists(PLUGINS_PATH))
        {
            hasQuarantine = false;
            return;
        }

        isChecking = true;
        Repaint();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xattr",
                Arguments = $"-r \"{PLUGINS_PATH}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogWarning($"[VLC Unity] xattr check stderr: {error}");
            }

            hasQuarantine = output.Contains("com.apple.quarantine");

            if (hasQuarantine)
            {
                command = $"sudo xattr -rd com.apple.quarantine \"{Path.GetFullPath(PLUGINS_PATH)}\"";
            }
            else
            {
                command = "";
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[VLC Unity] Failed to check quarantine status: {e.Message}");
            hasQuarantine = false;
            command = "";
        }
        finally
        {
            isChecking = false;
            Repaint();
        }
        #else
        hasQuarantine = false;
        command = "";
        #endif
    }

    private void RemoveQuarantine()
    {
        #if UNITY_EDITOR_OSX
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"do shell script \\\"xattr -rd com.apple.quarantine '{Path.GetFullPath(PLUGINS_PATH)}'\\\" with administrator privileges\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                EditorUtility.DisplayDialog(
                    "Success",
                    "Plugins are now authorized. VLC Unity should now work normally in the Editor.",
                    "OK"
                );

                CheckQuarantineStatus();
            }
            else
            {
                string errorMsg = string.IsNullOrEmpty(error) ? "User may have cancelled or permission was denied." : error;
                UnityEngine.Debug.LogWarning($"[VLC Unity] Failed to authorize plugins: {errorMsg}");

                EditorUtility.DisplayDialog(
                    "Error",
                    "Failed to authorize plugins.\n\n" + errorMsg,
                    "OK"
                );
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[VLC Unity] Failed to authorize plugins: {e.Message}");

            EditorUtility.DisplayDialog(
                "Error",
                "Failed to authorize plugins: " + e.Message,
                "OK"
            );
        }
        #endif
    }

    private void AddQuarantine()
    {
        #if UNITY_EDITOR_OSX
        if (!Directory.Exists(PLUGINS_PATH))
        {
            EditorUtility.DisplayDialog("Error", "Plugin path not found: " + PLUGINS_PATH, "OK");
            return;
        }

        string fullPath = Path.GetFullPath(PLUGINS_PATH);
        string addCommand = $"xattr -w com.apple.quarantine '0081;{System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()};UnityEditor;' \"{fullPath}\"/*";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{addCommand}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogWarning($"[VLC Unity] xattr add stderr: {error}");
            }

            EditorUtility.DisplayDialog(
                "Plugins Unauthorized",
                "Plugins are now blocked by macOS. Use 'Authorize Plugins' to restore access.",
                "OK"
            );

            CheckQuarantineStatus();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[VLC Unity] Failed to unauthorize plugins: {e.Message}");

            EditorUtility.DisplayDialog(
                "Error",
                "Failed to unauthorize plugins: " + e.Message,
                "OK"
            );
        }
        #endif
    }
}
