using System;
using System.Collections.Generic;
using System.Linq;
using PortAudioForUnity;
using UnityEngine;

public static class MicProfileUtils
{
    public static List<MicProfile> CreateMicProfiles(List<MicProfile> persistedMicProfiles, List<Color32> micProfileColors, List<IConnectedClientHandler> connectedClientHandlers)
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.

        // PortAudio returns too many recording devices. Thus, explicitly use the Unity API here to get available recording device names.
        List<string> connectedMicNames = Microphone.devices.ToList();
        List<MicProfile> micProfiles = new(persistedMicProfiles);
        List<Color32> usedMicProfileColors = persistedMicProfiles.Select(it => it.Color).ToList();
        
        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            try
            {
                MicrophoneAdapter.GetDeviceCaps(connectedMicName, out int minSampleRate, out int maxSampleRate, out int channelCount);
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
                Debug.LogError(ex);
                continue;
            }
        }

        // Create mic profiles for connected companion apps that are not yet in the list
        foreach (IConnectedClientHandler connectedClientHandler in connectedClientHandlers)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.ConnectedClientId == connectedClientHandler.ClientId && it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new(connectedClientHandler.ClientName, 0, connectedClientHandler.ClientId);
                micProfiles.Add(micProfile);
            }
        }

        micProfiles.Sort(MicProfile.compareByName);

        return micProfiles;
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
