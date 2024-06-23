using System;
using ProTrans;
using UnityEngine;

public class SettingsManager : AbstractSingletonBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        SettingsLoaderSaver = null;
    }

    public static SettingsManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SettingsManager>();

    public static ISettingsLoaderSaver settingsLoaderSaver;
    public static ISettingsLoaderSaver SettingsLoaderSaver
    {
        get => settingsLoaderSaver;
        set
        {
            settingsLoaderSaver = value;

            // Reset already loaded settings.
            if (DontDestroyOnLoadManager.Instance != null
                && Instance != null)
            {
                Instance.settings = null;
                Instance.LoadSettings();
            }
        }
    }

    private Settings settings;
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

    private NonPersistentSettings nonPersistentSettings;
    public NonPersistentSettings NonPersistentSettings
    {
        get
        {
            if (nonPersistentSettings == null)
            {
                nonPersistentSettings = new();
            }
            return nonPersistentSettings;
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // GetCurrentAppResolution may only be called from Start() and Awake(). This is why it is done here.
        Settings.ScreenResolution = ApplicationUtils.GetScreenResolution();
    }

    protected override void OnDisableSingleton()
    {
        SaveSettings();
    }

    private void InitSettingsLoaderSaverIfNotDoneYet()
    {
        if (SettingsLoaderSaver != null)
        {
            return;
        }
        SettingsLoaderSaver = new FileSettingsLoaderSaver();
        Debug.Log($"No {nameof(SettingsLoaderSaver)} set. Using new instance of {SettingsLoaderSaver.GetType()}.");
    }

    public void SaveSettings()
    {
        if (settings == null)
        {
            Debug.LogWarning("Failed to save settings. Settings are null.");
            return;
        }

        InitSettingsLoaderSaverIfNotDoneYet();
        SettingsUtils.SimplifySettings(settings);
        SettingsLoaderSaver.SaveSettings(settings);
    }

    private void LoadSettings()
    {
        if (settings != null)
        {
            throw new IllegalStateException("Settings have been loaded already");
        }

        InitSettingsLoaderSaverIfNotDoneYet();

        settings = SettingsLoaderSaver.LoadSettings();
        OverwriteSettingsWithCommandLineArguments(settings);

        // Update log level
        Debug.Log($"Using loaded log level: {settings.MinimumLogLevel}");
        Log.MinimumLogLevel = settings.MinimumLogLevel;

        Debug.Log($"Using loaded CultureInfo: {settings.CultureInfoName}");
        TranslationConfig.Singleton.CurrentCultureInfo = SettingsUtils.GetCultureInfo(settings);
    }

    private static void OverwriteSettingsWithCommandLineArguments(Settings settings)
    {
        string settingsOverwriteJson = ApplicationManager.Instance.GetCommandLineArgument("--settingsOverwriteJson");
        if (settingsOverwriteJson.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Overwriting loaded settings with command line argument: {settingsOverwriteJson}");
        settingsOverwriteJson = settingsOverwriteJson.Strip("\"", "\"");
        settingsOverwriteJson = settingsOverwriteJson.Strip("'", "'");
        try
        {
            JsonConverter.FillFromJson(settingsOverwriteJson, settings);
        }
        catch (Exception e)
        {
            Debug.LogError("OverwriteSettingsWithCommandLineArguments failed");
            Debug.LogException(e);
        }
    }
}
