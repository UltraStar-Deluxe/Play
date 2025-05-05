using System;
using RandomFriendlyNameGenerator;
using UnityEngine.Device;

public class SettingsUtils
{
    public static string GetInitialClientName()
    {
        try
        {
            string clientName = SystemInfo.deviceName;
            if (!clientName.IsNullOrEmpty()
                && clientName != SystemInfo.unsupportedIdentifier)
            {
                return clientName;
            }
        }
        catch (Exception e)
        {
            e.Log("Failed to get initial client name via Unity API. Using random name instead.");
        }

        return GetRandomClientName();
    }

    private static string GetRandomClientName()
    {
        return NameGenerator.Identifiers.Get(
                numberOfNamesToReturn: 10,
                components: IdentifierComponents.Adjective | IdentifierComponents.Animal,
                orderStyle: NameOrderingStyle.SilentBobStyle,
                separator: null,
                forceSingleLetter: true)
            // Take the shortest name
            .FindMinElement(name => name.Length);
    }
}
