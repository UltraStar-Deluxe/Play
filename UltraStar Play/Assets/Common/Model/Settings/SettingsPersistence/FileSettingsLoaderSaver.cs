using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public class FileSettingsLoaderSaver : ISettingsLoaderSaver
{
    private string settingsPath;
    public string SettingsPath
    {
        get
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

    public Settings LoadSettings()
    {
        Debug.Log($"Loading settings from file '{SettingsPath}'");
        using DisposableStopwatch stopwatch = new DisposableStopwatch("Loading the settings took <millis> ms");

        if (!File.Exists(SettingsPath))
        {
            Debug.LogWarning($"Settings file not found. Creating default settings at '{SettingsPath}'.");
            SaveSettings(DefaultSettingsFactory.CreateDefaultSettings());
        }

        string fileContent = File.ReadAllText(SettingsPath);
        try
        {
            return JsonConverter.FromJson<Settings>(fileContent);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            // Create copy of original settings file with timestamp
            string originalSettingsPath = SettingsPath;
            string dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd'T'HH-mm-ss", CultureInfo.InvariantCulture);
            string settingsCopyPath = originalSettingsPath.Replace(".json", $"_crashed_{dateTimeStamp}.json");
            File.WriteAllText(settingsCopyPath, fileContent);
            Debug.LogError($"Failed to load settings from JSON. Using new default settings instead. You can find the original settings in {settingsCopyPath}. Original settings JSON: {fileContent}");

            // Fall back to default settings
            return DefaultSettingsFactory.CreateDefaultSettings();
        }
    }

    public void SaveSettings(Settings settings)
    {
        try
        {
            string json = JsonConverter.ToJson(settings, true);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to save settings: {ex.Message}");
        }
    }
}
