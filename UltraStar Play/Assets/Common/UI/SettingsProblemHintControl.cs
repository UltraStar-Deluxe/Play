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
    private readonly VisualElement visualElement;
    private readonly TooltipControl tooltipControl;

    private bool hasIssues;
    
    public SettingsProblemHintControl(VisualElement visualElement, List<string> settingsProblems, Injector injector)
    {
        this.tooltipControl = new(visualElement);
        this.visualElement = visualElement;
        this.visualElement.style.scale = Vector2.zero;
        this.visualElement.SetVisibleByDisplay(!settingsProblems.IsNullOrEmpty());
        SetProblems(settingsProblems);
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

        // Check song folders
        bool hasDuplicateFolder = false;
        bool hasDuplicateSubfolder = false;
        foreach (string songFolder in settings.GameSettings.songDirs)
        {
            if (!hasDuplicateFolder
                && IsDuplicateFolder(songFolder, settings.GameSettings.songDirs))
            {
                hasDuplicateFolder = true;
                result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_duplicateSongFolders));
            }

            if (!hasDuplicateSubfolder
                && IsSubfolderOfAnyOtherFolder(songFolder, settings.GameSettings.songDirs, out string _))
            {
                hasDuplicateSubfolder = true;
                result.Add(TranslationManager.GetTranslation(R.Messages.settingsProblem_songFolderIsSubfolderOfOtherSongFolder));
            }
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

    public static bool IsDuplicateFolder(string path, List<string> folders)
    {
        if (path.IsNullOrEmpty()
            || !Directory.Exists(path))
        {
            return false;
        }

        return folders
            .Where(folder => !folder.IsNullOrEmpty() && Directory.Exists(folder))
            // FullName returns the normalized absolute path.
            .Count(folder => new DirectoryInfo(folder).FullName == new DirectoryInfo(path).FullName) >= 2;
    }

    public static bool IsSubfolderOfAnyOtherFolder(string potentialSubfolder, List<string> potentialParentFolders, out string parentFolder)
    {
        foreach (string potentialParentFolder in potentialParentFolders)
        {
            if (potentialSubfolder != potentialParentFolder
                && IsSubfolder(potentialSubfolder, potentialParentFolder))
            {
                parentFolder = potentialParentFolder;
                return true;
            }
        }

        parentFolder = "";
        return false;
    }

    private static bool IsSubfolder(string potentialSubfolder, string potentialParentFolder)
    {
        if (potentialSubfolder.IsNullOrEmpty() || potentialParentFolder.IsNullOrEmpty())
        {
            return false;
        }

        DirectoryInfo potentialSubfolderInfo = new(potentialSubfolder);
        DirectoryInfo potentialParentInfo = new(potentialParentFolder);

        // FullName returns the normalized absolute path.
        if (potentialSubfolderInfo.FullName == potentialParentInfo.FullName)
        {
            // Equal paths do not count as subfolder.
            return false;
        }

        // Additional slash at the end to only check the full name, not any parts of it.
        string potentialParentAbsolutePath = potentialParentInfo.FullName;
        if (!potentialParentAbsolutePath.EndsWith(Path.DirectorySeparatorChar))
        {
            potentialParentAbsolutePath += Path.DirectorySeparatorChar;
        }
        return potentialSubfolderInfo.FullName.StartsWith(potentialParentAbsolutePath);
    }

    public void SetProblems(List<string> settingsProblems)
    {
        bool hasIssues = !settingsProblems.IsNullOrEmpty();
        if (hasIssues
            && !this.hasIssues)
        {
            visualElement.ShowByDisplay();
            LeanTween.value(GameObjectUtils.FindAnyGameObject(), 0, 1, 1f)
                .setOnUpdate(value =>
                {
                    visualElement.style.scale = new Vector2(value, value);
                })
                .setEaseSpring();
        }
        else if (!hasIssues
                 && this.hasIssues)
        {
            LeanTween.value(GameObjectUtils.FindAnyGameObject(), 1, 0, 0.3f)
                .setOnUpdate(value =>
                {
                    visualElement.style.scale = new Vector2(value, value);
                })
                .setEaseLinear();
        }
        this.hasIssues = hasIssues;
        
        tooltipControl.TooltipText = settingsProblems.JoinWith("\n\n");
    }
}
