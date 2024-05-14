using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MicProfileUtils
{
    public static List<MicProfile> CreateAndPersistMicProfiles(
        Settings settings,
        ThemeManager themeManager,
        ServerSideCompanionClientManager serverSideCompanionClientManager)
    {
        List<MicProfile> persistedMicProfiles = settings.MicProfiles;
        List<Color32> microphoneColors = themeManager.GetMicrophoneColors();
        List<ICompanionClientHandler> companionClientHandlers = serverSideCompanionClientManager.GetAllCompanionClientHandlers();
        List<MicProfile> micProfiles = CreateMicProfiles(persistedMicProfiles, microphoneColors, companionClientHandlers, settings);
        micProfiles.Sort(MicProfile.compareByName);

        List<MicProfile> newMicProfiles = micProfiles
            .Except(persistedMicProfiles)
            .ToList();
        if (!newMicProfiles.IsNullOrEmpty())
        {
            string micProfileNamesCsv = newMicProfiles
                .Select(micProfile => micProfile.GetDisplayNameWithChannel())
                .JoinWith(", ");
            Debug.Log($"Found new mics: {micProfileNamesCsv}");
            settings.MicProfiles.AddRange(newMicProfiles);
            settings.MicProfiles.Sort(MicProfile.compareByName);
        }

        return micProfiles;
    }

    public static List<MicProfile> CreateMicProfiles(List<MicProfile> persistedMicProfiles, List<Color32> micProfileColors, List<ICompanionClientHandler> companionClientHandlers, Settings settings)
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.

        List<string> connectedMicNames = GetConnectedMicrophoneNames();
        List<MicProfile> micProfiles = new(persistedMicProfiles);
        List<Color32> usedMicProfileColors = persistedMicProfiles.Select(it => it.Color).ToList();

        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            try
            {
                IMicrophoneAdapter.Instance.GetDeviceCaps(connectedMicName, out int minSampleRate, out int maxSampleRate, out int channelCount);
                for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    bool alreadyInList = micProfiles.AnyMatch(it =>
                        it.Name == connectedMicName
                        && it.ChannelIndex == channelIndex
                        && !it.IsInputFromConnectedClient);
                    if (!alreadyInList)
                    {
                        MicProfile micProfile = new(connectedMicName, channelIndex);

                        micProfile.Color = GetUnusedMicProfileColor(micProfileColors, usedMicProfileColors);
                        usedMicProfileColors.Add(micProfile.Color);

                        micProfiles.Add(micProfile);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Create MicProfile failed: {ex.Message}");
            }
        }

        // Create mic profiles for connected companion apps that are not yet in the list
        foreach (ICompanionClientHandler companionClientHandler in companionClientHandlers)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.ConnectedClientId == companionClientHandler.ClientId && it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new(companionClientHandler.ClientName, 0, companionClientHandler.ClientId);
                micProfiles.Add(micProfile);
            }
        }

        micProfiles.Sort(MicProfile.compareByName);

        return micProfiles;
    }

    private static List<string> GetConnectedMicrophoneNames()
    {
        // Some obscure devices contain weird characters that may cause issues.
        // Example: 'Input (@System32\drivers\bthhfenum.sys,#4;%1 Hands-Free HF Audio%0\r\n;(Galaxy S10e))'
        return IMicrophoneAdapter.Instance.Devices
            .Where(deviceName => !deviceName.Contains("\r")
                                 && !deviceName.Contains("\n"))
            .ToList();
    }

    private static Color32 GetUnusedMicProfileColor(List<Color32> micProfileColors, List<Color32> usedMicProfileColors)
    {
        List<Color32> unusedMicProfileColors = micProfileColors.Except(usedMicProfileColors).ToList();
        if (unusedMicProfileColors.IsNullOrEmpty())
        {
            return RandomUtils.RandomColor();
        }

        return RandomUtils.RandomOf(unusedMicProfileColors);
    }
}
