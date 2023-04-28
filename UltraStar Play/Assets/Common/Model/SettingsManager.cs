using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
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
                LoadSettings();
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
        SimplifySettings();

        string json = JsonConverter.ToJson(Settings, true);
        File.WriteAllText(GetSettingsPath(), json);
    }

    private void SimplifySettings()
    {
        // Remove permission list if empty
        List<string> clientIdsWithoutPermission = Settings.HttpApiPermissions
            .Where(entry => entry.Value.IsNullOrEmpty())
            .Select(entry => entry.Key)
            .ToList();
        clientIdsWithoutPermission.ForEach(clientId => Settings.HttpApiPermissions.Remove(clientId));
    }

    private void LoadSettings()
    {
        using (new DisposableStopwatch("Loading the settings took <millis> ms"))
        {
            string loadedSettingsPath = GetSettingsPath();
            if (!File.Exists(loadedSettingsPath))
            {
                Debug.LogWarning($"Settings file not found. Creating default settings at {loadedSettingsPath}.");
                settings = CreateDefaultSettings();
                Save();
                return;
            }

            string fileContent = File.ReadAllText(loadedSettingsPath);
            try
            {
                settings = JsonConverter.FromJson<Settings>(fileContent);
            }
            catch (Exception ex)
            {
                string settingsCopyPath = GetSettingsPath().Replace(".json", "_crash.json");
                File.WriteAllText(settingsCopyPath, fileContent);
                Debug.LogError(ex);
                Debug.LogError($"Failed to load settings from JSON. Using new default settings instead. You can find the original settings in {settingsCopyPath}. Original settings JSON: {fileContent}");
                settings = CreateDefaultSettings();
            }
            OverwriteSettingsWithCommandLineArguments();

            ResetNonPersistentSettings();
        }
    }

    private void ResetNonPersistentSettings()
    {
        // TODO: Store non-persistent settings in dedicated data structure.
        settings.GameRoundSettings = new();
        
        settings.SongSelectSettings.playlistName = "";
        settings.SongSelectSettings.micTestActive = false;
        settings.activeSearchPropertyFilters = new();
        settings.isShowOnlyDuetsFilterActive = false;
        
        settings.SongEditorSettings.MusicPlaybackSpeed = 1;
        settings.SongEditorSettings.IsRecordingEnabled = false;
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

        // Add player profiles
        defaultSettings.PlayerProfiles.Add(new PlayerProfile("Player01", EDifficulty.Medium, "01-UltraStar-chan/ultrastar-chan-f-closeup.png"));
        defaultSettings.PlayerProfiles.Add(new PlayerProfile("Player02", EDifficulty.Medium, "01-UltraStar-chan/ultrastar-chan-m-closeup.png"));
        
        // Add mic profiles
        try
        {
            ThemeManager themeManager = ThemeManager.Instance;
            ThemeJson defaultThemeJson = themeManager.GetDefaultTheme().ThemeJson;
            List<Color32> micProfileColors = themeManager.GetMicrophoneColors(defaultThemeJson);
            
            List<IConnectedClientHandler> connectedClientHandlers = new List<IConnectedClientHandler>();
            List<MicProfile> persistedMicProfiles = new();
            
            defaultSettings.MicProfiles = MicProfileUtils.CreateMicProfiles(persistedMicProfiles, micProfileColors, connectedClientHandlers);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to create initial mic profiles");
        }
        
        // Set first player profile name to Steam account name.
        SteamManager steamManager = SteamManager.Instance;
        if (steamManager.IsConnectedToSteam)
        {
            defaultSettings.PlayerProfiles.FirstOrDefault().Name = steamManager.PlayerName;
        }
        else
        {
            // Wait a little bit until connection to Steam has been established.
            float startTimeInSeconds = Time.time;
            steamManager.ConnectedToSteamEventStream
                .Where(_ => Time.time - startTimeInSeconds < 10)
                .Subscribe(_ => defaultSettings.PlayerProfiles.FirstOrDefault().Name = steamManager.PlayerName);
        }

        return defaultSettings;
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
                Debug.LogError("OverwriteSettingsWithCommandLineArguments failed");
                Debug.LogException(e);
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
