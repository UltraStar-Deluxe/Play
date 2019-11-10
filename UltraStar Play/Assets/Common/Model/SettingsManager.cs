using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SettingsManager>("SettingsManager");
        }
    }

    private readonly string settingsPath = "Settings.json";

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

    // Non-static settings field for debugging of the settings in the Unity Inspector.
    public Settings nonStaticSettings;

    void Start()
    {
        nonStaticSettings = settings;
    }

    void OnDisable()
    {
        Save();
    }

    public void Save()
    {
        string json = JsonConverter.ToJson(Settings, true);
        File.WriteAllText(settingsPath, json);
    }

    public void Reload()
    {
        string fileContent = File.ReadAllText(settingsPath);
        settings = JsonConverter.FromJson<Settings>(fileContent);
        nonStaticSettings = settings;
    }
}
