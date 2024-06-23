using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NHyphenator;
using NHyphenator.Loaders;
using UnityEngine;

public static class SettingsUtils
{
    private const string DefaultSpeechRecognitionModelPathInStreamingAssets = "SpeechRecognitionModels/WhisperModels/ggml-tiny.bin";
    private const string DefaultSpeechRecognitionLanguage = "auto";

    public static CultureInfo GetCultureInfo(ISettings settings)
    {
        try
        {
            return new CultureInfo(settings.CultureInfoName);
        }
        catch (Exception ex)
        {
            return new CultureInfo("en");
        };
    }

    public static void SimplifySettings(Settings settings)
    {
        // Remove permission list if empty
        List<string> clientIdsWithoutPermission = settings.HttpApiPermissions
            .Where(entry => entry.Value.IsNullOrEmpty())
            .Select(entry => entry.Key)
            .ToList();
        clientIdsWithoutPermission.ForEach(clientId => settings.HttpApiPermissions.Remove(clientId));
    }

    public static bool ShouldUsePortAudio(Settings settings)
    {
        return settings.PreferPortAudio
               && ApplicationUtils.CanUsePortAudio();
    }

    public static List<HttpApiPermission> GetPermissions(Settings settings, string clientId)
    {
        if (clientId.IsNullOrEmpty())
        {
            return new();
        }

        if (settings.HttpApiPermissions.TryGetValue(clientId, out List<HttpApiPermission> permissions))
        {
            return permissions;
        }

        return new();
    }

    public static List<string> GetEnabledSongFolders(Settings settings)
    {
        if (settings == null)
        {
            return new List<string>();
        }

        return settings.SongDirs
            .Except(settings.DisabledSongFolders)
            .ToList();
    }

    public static void AddPermission(Settings settings, string clientId, HttpApiPermission permission)
    {
        if (!settings.HttpApiPermissions.ContainsKey(clientId))
        {
            settings.HttpApiPermissions[clientId] = new();
        }
        settings.HttpApiPermissions[clientId].AddIfNotContains(permission);
    }

    public static void RemovePermission(Settings settings, string clientId, HttpApiPermission permission)
    {
        if (!settings.HttpApiPermissions.ContainsKey(clientId))
        {
            return;
        }
        settings.HttpApiPermissions[clientId].Remove(permission);
    }

    public static List<PlayerProfile> GetPlayerProfiles(Settings settings, NonPersistentSettings nonPersistentSettings)
    {
        if (!nonPersistentSettings.LobbyMemberPlayerProfiles.IsNullOrEmpty())
        {
            return nonPersistentSettings.LobbyMemberPlayerProfiles
                .Cast<PlayerProfile>()
                .ToList();
        }
        else
        {
            return settings.PlayerProfiles;
        }
    }

    public static PlayerProfile GetPlayerProfile(Settings settings, NonPersistentSettings nonPersistentSettings, string profileName)
    {
        return GetPlayerProfiles(settings, nonPersistentSettings)
            .FirstOrDefault(playerProfile => playerProfile.Name == profileName);
    }

    public static List<MicProfile> GetMicProfiles(Settings settings, string profileName)
    {
        return settings.MicProfiles
            .Where(micProfile => micProfile.Name == profileName)
            .ToList();
    }

    public static MicProfile GetMicProfile(Settings settings, string profileName, int channelIndex)
    {
        return settings.MicProfiles
            .FirstOrDefault(micProfile => micProfile.Name == profileName
                                          && micProfile.ChannelIndex == channelIndex);
    }

    public static bool ShouldAnimateSceneChange(Settings settings)
    {
        return settings.SceneChangeDurationInSeconds > 0;
    }

    public static List<MicProfile> GetAvailableMicProfiles(Settings settings, ThemeManager themeManager, ServerSideCompanionClientManager serverSideCompanionClientManager)
    {
        List<MicProfile> allMicProfiles = MicProfileUtils.CreateAndPersistMicProfiles(settings, themeManager, serverSideCompanionClientManager);

        return allMicProfiles
            .Where(it => it.IsEnabledAndConnected(serverSideCompanionClientManager))
            .ToList();
    }

    public static void IncreaseVolume(Settings settings)
    {
        settings.VolumePercent += 10;
        settings.VolumePercent = NumberUtils.Limit(settings.VolumePercent, 0, 100);
    }

    public static void DecreaseVolume(Settings settings)
    {
        settings.VolumePercent -= 10;
        settings.VolumePercent = NumberUtils.Limit(settings.VolumePercent, 0, 100);
    }

    public static Hyphenator CreateHyphenator(Settings settings)
    {
        string speechRecognitionLanguage = settings?.SongEditorSettings?.SpeechRecognitionLanguage;
        if (speechRecognitionLanguage.IsNullOrEmpty())
        {
            return null;
        }

        IHyphenatePatternsLoader hyphenatePatternsLoader = HyphenationPatternsProvider.CreateHyphenationPatternsLoader(speechRecognitionLanguage);
        if (hyphenatePatternsLoader == null)
        {
            Debug.LogWarning("No hyphenation patterns found for language: " + speechRecognitionLanguage);
            return null;
        }

        Hyphenator hyphenator = new Hyphenator(
            hyphenatePatternsLoader,
            EditLyricsUtils.syllableSeparator,
            5,
            0,
            true,
            true);
        return hyphenator;
    }

    public static string GetGeneratedSongFolderAbsolutePath(Settings settings)
    {
        if (settings.GeneratedFolderPath.IsNullOrEmpty())
        {
            return ApplicationUtils.GetPersistentDataPath($"{ApplicationUtils.GeneratedFolderName}/Songs");
        }
        else
        {
            return settings.GeneratedFolderPath;
        }
    }

    public static Encoding GetEncodingForWritingUltraStarTxtFile(Settings settings)
    {
        return EncodingUtils.GetUtf8Encoding(settings.WriteUltraStarTxtFileWithByteOrderMark);
    }

    public static string GetSpeechRecognitionModelPath(Settings settings)
    {
        string modelPath = settings.SongEditorSettings.SpeechRecognitionModelPath;
        if (modelPath.IsNullOrEmpty())
        {
            return ApplicationUtils.GetStreamingAssetsPath(DefaultSpeechRecognitionModelPathInStreamingAssets);
        }
        return modelPath;
    }

    public static string GetSpeechRecognitionLanguage(Settings settings)
    {
        string language = settings.SongEditorSettings.SpeechRecognitionLanguage;
        if (language.IsNullOrEmpty())
        {
            return DefaultSpeechRecognitionLanguage;
        }
        return language;
    }

    public static UltraStarSongFormatVersion GetUltraStarSongFormatVersionForSave(Settings settings, UltraStarSongFormatVersion currentVersion)
    {
        if (currentVersion.EnumValue is EUltraStarSongFormatVersion.Unknown)
        {
            return GetDefaultUltraStarSongFormatVersionForSave(settings.DefaultUltraStarSongFormatVersionForSave);
        }

        UltraStarSongFormatVersion upgradeVersion = GetUpgradeUltraStarSongFormatVersionForSave(settings.UpgradeUltraStarSongFormatVersionForSave);
        if (settings.UpgradeUltraStarSongFormatVersionForSave is not EUpgradeUltraStarSongFormatVersion.None
            && (int)currentVersion.EnumValue < (int)upgradeVersion.EnumValue)
        {
            return upgradeVersion;
        }

        return currentVersion;
    }

    private static UltraStarSongFormatVersion GetDefaultUltraStarSongFormatVersionForSave(EKnownUltraStarSongFormatVersion defaultVersion)
    {
        switch (defaultVersion)
        {
            case EKnownUltraStarSongFormatVersion.V100: return UltraStarSongFormatVersion.v100;
            case EKnownUltraStarSongFormatVersion.V110: return UltraStarSongFormatVersion.v110;
            case EKnownUltraStarSongFormatVersion.V120: return UltraStarSongFormatVersion.v120;
            case EKnownUltraStarSongFormatVersion.V200: return UltraStarSongFormatVersion.v200;
            default:
                throw new IllegalArgumentException($"No mapping from default version for save {defaultVersion} to actual UltraStar format version");
        }
    }

    private static UltraStarSongFormatVersion GetUpgradeUltraStarSongFormatVersionForSave(EUpgradeUltraStarSongFormatVersion upgradeVersion)
    {
        switch (upgradeVersion)
        {
            case EUpgradeUltraStarSongFormatVersion.None: return UltraStarSongFormatVersion.unknown;
            case EUpgradeUltraStarSongFormatVersion.V110: return UltraStarSongFormatVersion.v110;
            case EUpgradeUltraStarSongFormatVersion.V120: return UltraStarSongFormatVersion.v120;
            case EUpgradeUltraStarSongFormatVersion.V200: return UltraStarSongFormatVersion.v200;
            default:
                throw new IllegalArgumentException($"No mapping from upgrade version for save {upgradeVersion} to actual UltraStar format version");
        }
    }
}
