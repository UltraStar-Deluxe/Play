using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// The SettingsManager
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SettingsManager>("SettingsManager");
        }
    }

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

    // Non-static settings field for debugging of the settings in the Unity Inspector.
    public Settings nonStaticSettings;

    void Start()
    {
        // Load reference from last scene if needed
        nonStaticSettings = settings;
        if (!initializedResolution)
        {
            initializedResolution = true;
            // GetCurrentAppResolution may only be called from Start() and Awake(). This is why it is done here.
            Settings.GraphicSettings.resolution = ApplicationUtils.GetCurrentAppResolution();
        }
    }

    void OnDisable()
    {
        Save();
    }

    public void Save()
    {
        string json = JsonConverter.ToJson(Settings, true);
        File.WriteAllText(SettingsPath(), json);
    }

    public void Reload()
    {
        using (new DisposableStopwatch("Loading the settings took <millis> ms"))
        {
            string settingsPath = SettingsPath();
            if (!File.Exists(settingsPath))
            {
                UnityEngine.Debug.LogWarning($"Settings file not found. Creating default settings at {settingsPath}.");
                settings = new Settings();
                Save();
                return;
            }
            string fileContent = File.ReadAllText(settingsPath);
            settings = JsonConverter.FromJson<Settings>(fileContent);
            nonStaticSettings = settings;
        }
    }

    public string SettingsPath()
    {
        return Path.Combine(Application.persistentDataPath, "Settings.json");
    }
}
