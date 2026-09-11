using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProTrans;
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
            defaultSettings.DisabledSongFolders.Add(demoSongFolder);
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

            List<ICompanionClientHandler> companionClientHandlers = new List<ICompanionClientHandler>();
            List<MicProfile> persistedMicProfiles = new();

            defaultSettings.MicProfiles = MicProfileUtils.CreateMicProfiles(persistedMicProfiles, micProfileColors, companionClientHandlers, defaultSettings);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed to create initial mic profiles");
        }

        return defaultSettings;
    }
}
