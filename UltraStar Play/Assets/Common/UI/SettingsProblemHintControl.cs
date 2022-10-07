using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SettingsProblemHintControl
{
    public SettingsProblemHintControl(VisualElement visualElement, List<string> settingsProblems, Injector injector)
    {
        visualElement.SetVisibleByDisplay(!settingsProblems.IsNullOrEmpty());
        TooltipControl settingsProblemTooltipControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<TooltipControl>();
        settingsProblemTooltipControl.TooltipText = settingsProblems.JoinWith("\n\n");
    }

    public static List<string> GetAllSettingsProblems(Settings settings)
    {
        return GetSongLibrarySettingsProblems(settings)
            .Concat(GetRecordingSettingsProblems(settings))
            .Concat(GetPlayerSettingsProblems(settings))
            .ToList();
    }

    public static List<string> GetSongLibrarySettingsProblems(Settings settings)
    {
        List<string> result = new();
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            result.Add("Add a song folder");
        }
        else if (settings.GameSettings.songDirs.AnyMatch(songDir => !Directory.Exists(songDir)))
        {
            result.Add("At least one song folder does not exist");
        }
        return result;
    }

    public static List<string> GetRecordingSettingsProblems(Settings settings)
    {
        List<string> result = new();
        if (settings.MicProfiles.IsNullOrEmpty()
            || !settings.MicProfiles
                .AnyMatch(micProfile => micProfile.IsEnabled))
        {
            result.Add("Select a microphone for singing");
        }
        else if (settings.MicProfiles
                 .AllMatch(micProfile => !micProfile.IsEnabled || !micProfile.IsConnected(ServerSideConnectRequestManager.Instance)))
        {
            result.Add("Connect a microphone\nor select another one for singing");
        }
        return result;
    }

    public static List<string> GetPlayerSettingsProblems(Settings settings)
    {
        List<string> result = new();
        if (settings.PlayerProfiles.IsNullOrEmpty())
        {
            result.Add("Add a player profile for singing");
        }
        else if (!settings.PlayerProfiles.AnyMatch(playerProfile => playerProfile.IsEnabled))
        {
            result.Add("Select a player profile for singing");
        }
        return result;
    }
}
