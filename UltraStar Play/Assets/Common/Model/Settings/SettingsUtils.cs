using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHyphenator;
using NHyphenator.Loaders;
using UnityEngine;

public static class SettingsUtils
{
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

    public static PlayerProfile GetPlayerProfile(Settings settings, string profileName)
    {
        return settings.PlayerProfiles.FirstOrDefault(playerProfile => playerProfile.Name == profileName);
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

    public static List<MicProfile> GetAvailableMicProfiles(Settings settings, ThemeManager themeManager, ServerSideConnectRequestManager serverSideConnectRequestManager)
    {
        List<MicProfile> allMicProfiles = MicProfileUtils.CreateAndPersistMicProfiles(settings, themeManager, serverSideConnectRequestManager);

        return allMicProfiles
            .Where(it => it.IsEnabledAndConnected(serverSideConnectRequestManager))
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
}
