using System;
using System.IO;
using UnityEngine;

public class SettingsManager : AbstractSingletonBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        settingsPath = null;
        settings = null;
    }

    public static SettingsManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SettingsManager>("SettingsManager");
        }
    }

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
                LoadSettings();
            }
            return settings;
        }
    }

    // Non-static settings field for debugging of the settings in the Unity Inspector.
    public Settings nonStaticSettings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // Load reference from last scene if needed
        nonStaticSettings = settings;
    }

    protected override void OnDisableSingleton()
    {
        Save();
    }

    protected override void OnDestroySingleton()
    {
        Save();
    }

    private void OnApplicationPause(bool isApplicationPaused)
    {
        if (isApplicationPaused)
        {
            Save();
        }
    }

    public void Save()
    {
        string json = JsonConverter.ToJson(Settings, true);
        File.WriteAllText(GetSettingsPath(), json);
    }

    private void LoadSettings()
    {
        using (new DisposableStopwatch("Loading the settings took <millis> ms"))
        {
            string loadedSettingsPath = GetSettingsPath();
            if (!File.Exists(loadedSettingsPath))
            {
                UnityEngine.Debug.LogWarning($"Settings file not found. Creating default settings at {loadedSettingsPath}.");
                settings = new Settings();
                // Create unique device identifier
                settings.CreateAndSetClientId();
                Save();
                return;
            }
            string fileContent = File.ReadAllText(loadedSettingsPath);
            settings = JsonConverter.FromJson<Settings>(fileContent);
            // ClientId was not set yet. Create a new one.
            if (settings.ClientId.IsNullOrEmpty())
            {
                settings.CreateAndSetClientId();
                Save();
            }
            nonStaticSettings = settings;
            OverwriteSettingsWithCommandLineArguments();
            Debug.Log($"ClientId: {settings.ClientId}");

            // Update log level
            Log.MinimumLogLevel = settings.MinimumLogLevel;
        }
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
