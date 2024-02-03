using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public static class DefaultSettingsFactory
{
    public static Settings CreateDefaultSettings()
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

                defaultSettings.SongDirs.Add(internalSongFolder);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Failed to create initial song folder: {ex.Message}");
            }
        }
#endif

        // Add song folder for demo song package
        string demoSongFolder = ApplicationUtils.GetDemoSongFolderAbsolutePath();
        if (DirectoryUtils.Exists(demoSongFolder))
        {
            defaultSettings.SongDirs.Add(demoSongFolder);
        }

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

            defaultSettings.MicProfiles = MicProfileUtils.CreateMicProfiles(persistedMicProfiles, micProfileColors, connectedClientHandlers, defaultSettings);
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
            // Set the player name when connection to Steam has been established.
            float startTimeInSeconds = Time.time;
            steamManager.ConnectedToSteamEventStream
                .Where(_ => Time.time - startTimeInSeconds < 10)
                .Subscribe(_ => defaultSettings.PlayerProfiles.FirstOrDefault().Name = steamManager.PlayerName);
        }

        // Set speech recognition model
        if (PlatformUtils.IsStandalone)
        {
            defaultSettings.SongEditorSettings.SpeechRecognitionModelPath =
                ApplicationUtils.GetStreamingAssetsPath("SpeechRecognitionModels/WhisperModels/ggml-tiny.bin");
        }

        return defaultSettings;
    }
}
