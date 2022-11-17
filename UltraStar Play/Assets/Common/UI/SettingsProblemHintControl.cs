using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProTrans;
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

    public static List<string> GetAllSettingsProblems(Settings settings, SongMetaManager songMetaManager)
    {
        return GetSongLibrarySettingsProblems(settings, songMetaManager)
            .Concat(GetRecordingSettingsProblems(settings))
            .Concat(GetPlayerSettingsProblems(settings))
            .ToList();
    }

    public static List<string> GetSongLibrarySettingsProblems(Settings settings, SongMetaManager songMetaManager)
    {
        List<string> result = new();
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_noSongFolders));
        }
        else if (settings.GameSettings.songDirs.AnyMatch(songDir => !Directory.Exists(songDir)))
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_songFolderDoesNotExist));
        }

        if (songMetaManager.GetSongIssues().Count > 0)
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_thereAreSongIssues));
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
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_noMicProfiles));
        }
        else if (settings.MicProfiles
                 .AllMatch(micProfile => !micProfile.IsEnabled || !micProfile.IsConnected(ServerSideConnectRequestManager.Instance)))
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_noConnectedAndEnabledMicProfile));
        }
        return result;
    }

    public static List<string> GetPlayerSettingsProblems(Settings settings)
    {
        List<string> result = new();
        if (settings.PlayerProfiles.IsNullOrEmpty())
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_noPlayerProfile));
        }
        else if (!settings.PlayerProfiles.AnyMatch(playerProfile => playerProfile.IsEnabled))
        {
            result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_noEnabledPlayerProfile));
        }
        return result;
    }
}
