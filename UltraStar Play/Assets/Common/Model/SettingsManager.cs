using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class SettingsManager : AbstractSingletonBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        settingsPath = null;
        settings = null;
        initializedResolution = false;
    }

    public static SettingsManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SettingsManager>();

    // The settings must be written to the same path they have been loaded from.
    // This field stores the path from where settings have been loaded / will be saved.
    private static string settingsPath;

    // The settings field is static to persist it across scene changes.
    // The SettingsManager is meant to be used as a singleton, such that this static field should not be a problem.
    private static Settings settings;
    public Settings Settings
    {
        get
        {
            if (settings == null)
            {
                Reload();
            }
            return settings;
        }
    }

    private static bool initializedResolution;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // Load reference from last scene if needed
        if (!initializedResolution)
        {
            initializedResolution = true;
            // GetCurrentAppResolution may only be called from Start() and Awake(). This is why it is done here.
            Settings.GraphicSettings.resolution = ApplicationUtils.GetScreenResolution();
        }
    }

    protected override void OnDisableSingleton()
    {
        Save();
    }

    public void Save()
    {
        string json = JsonConverter.ToJson(Settings, true);
        File.WriteAllText(GetSettingsPath(), json);
    }

    private void Reload()
    {
        using (new DisposableStopwatch("Loading the settings took <millis> ms"))
        {
            string loadedSettingsPath = GetSettingsPath();
            if (!File.Exists(loadedSettingsPath))
            {
                UnityEngine.Debug.LogWarning($"Settings file not found. Creating default settings at {loadedSettingsPath}.");
                settings = CreateDefaultSettings();
                Save();
                return;
            }
            string fileContent = File.ReadAllText(loadedSettingsPath);
            settings = JsonConverter.FromJson<Settings>(fileContent);
            OverwriteSettingsWithCommandLineArguments();
        }
    }

    private Settings CreateDefaultSettings()
    {
        Settings defaultSettings = new Settings();
#if UNITY_ANDROID
        if (!Application.isEditor)
        {
            // Create internal song folder on Android and add it to the settings.
            try
            {
                string internalSongFolder = AndroidUtils.GetAppSpecificStorageAbsolutePath(false) + "/Songs";
                if (!Directory.Exists(internalSongFolder))
                {
                    Directory.CreateDirectory(internalSongFolder);
                }

                defaultSettings.GameSettings.songDirs.Add(internalSongFolder);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to create initial song folder.");
                Debug.LogError(ex);
            }
        }
#endif

        // Try to select the first mic for singing.
        try
        {
            MicProfile defaultMicProfile = CreateDefaultMicProfile();
            if (defaultMicProfile != null)
            {
                settings.MicProfiles.Add(defaultMicProfile);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to create initial recording device profile.");
            Debug.LogError(ex);
        }

        return defaultSettings;
    }

    private MicProfile CreateDefaultMicProfile()
    {
        if (Microphone.devices.Length <= 0)
        {
            return null;
        }

        MicProfile result = new(Microphone.devices.FirstOrDefault());
        result.IsEnabled = true;
        return result;
    }

    private void OverwriteSettingsWithCommandLineArguments()
    {
        string settingsOverwriteJson = ApplicationManager.Instance.GetCommandLineArgument("--settingsOverwriteJson");
        if (!settingsOverwriteJson.IsNullOrEmpty())
        {
            settingsOverwriteJson = settingsOverwriteJson.Strip("\"", "\"");
            settingsOverwriteJson = settingsOverwriteJson.Strip("'", "'");
            try
            {
                JsonConverter.FillFromJson(settingsOverwriteJson, settings);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("OverwriteSettingsWithCommandLineArguments failed");
                UnityEngine.Debug.LogException(e);
            }
        }
    }

    public string GetSettingsPath()
    {
        if (settingsPath.IsNullOrEmpty())
        {
            string commandLineSettingsPath = ApplicationManager.Instance.GetCommandLineArgument("--settingsPath");
            commandLineSettingsPath = commandLineSettingsPath.Strip("\"", "\"");
            commandLineSettingsPath = commandLineSettingsPath.Strip("'", "'");
            if (!commandLineSettingsPath.IsNullOrEmpty())
            {
                settingsPath = commandLineSettingsPath;
            }
            else
            {
                settingsPath = Path.Combine(Application.persistentDataPath, "Settings.json");
            }
        }
        return settingsPath;
    }
}
