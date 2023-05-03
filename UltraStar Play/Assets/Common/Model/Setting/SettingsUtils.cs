using System.Collections.Generic;
using System.Linq;

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

    public static MicProfile GetMicProfile(Settings settings, string profileName, int channelIndex)
    {
        return settings.MicProfiles.FirstOrDefault(micProfile => micProfile.Name == profileName
                                                                 && micProfile.ChannelIndex == channelIndex);
    }

    public static bool ShouldAnimateSceneChange(Settings settings)
    {
        return settings.sceneChangeDurationInSeconds > 0;
    }
}
