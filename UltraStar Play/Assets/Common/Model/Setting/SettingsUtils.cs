using System.Collections.Generic;
using System.IO;
using System.Linq;
using NHyphenator;
using NHyphenator.Loaders;
using UnityEngine;

public static class SettingsUtils
{
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
}
