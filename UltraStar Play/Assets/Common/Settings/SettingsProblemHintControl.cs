using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SettingsProblemHintControl
{
    private readonly VisualElement visualElement;
    private readonly TooltipControl tooltipControl;

    private bool hasIssues;

    public SettingsProblemHintControl(VisualElement visualElement, List<Translation> settingsProblems)
    {
        this.tooltipControl = new(visualElement);
        this.visualElement = visualElement;
        this.visualElement.style.scale = Vector2.zero;
        this.visualElement.SetVisibleByDisplay(!settingsProblems.IsNullOrEmpty());
        SetProblems(settingsProblems);
    }

    public static List<Translation> GetAllSettingsProblems(
        Settings settings,
        ModManager modManager,
        SongIssueManager songIssueManager)
    {
        return GetSongLibrarySettingsProblems(settings, songIssueManager)
            .Concat(GetRecordingSettingsProblems(settings))
            .Concat(GetPlayerSettingsProblems(settings))
            .Concat(GetModSettingsProblems(modManager))
            .ToList();
    }

    public static List<Translation> GetModSettingsProblems(ModManager modManager)
    {
        List<Translation> result = new();
        if (!modManager.EnabledFailedToLoadModFolders.IsNullOrEmpty())
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_modFailedToLoad));
        }

        return result;
    }

    public static List<Translation> GetSongLibrarySettingsProblems(Settings settings, SongIssueManager songIssueManager)
    {
        List<Translation> result = new();
        if (settings.SongDirs.IsNullOrEmpty())
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_noSongFolders));
        }
        else if (settings.SongDirs.AnyMatch(songDir => !Directory.Exists(songDir)))
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_songFolderDoesNotExist));
        }

        if (songIssueManager.HasSongIssues)
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_thereAreSongIssues));
        }

        // Check song folders
        bool hasDuplicateFolder = false;
        bool hasDuplicateSubfolder = false;
        foreach (string songFolder in settings.SongDirs)
        {
            if (!hasDuplicateFolder
                && IsDuplicateFolder(songFolder, settings.SongDirs))
            {
                hasDuplicateFolder = true;
                result.Add(Translation.Get(R.Messages.settingsProblem_duplicateSongFolders));
            }

            if (!hasDuplicateSubfolder
                && IsSubfolderOfAnyOtherFolder(songFolder, settings.SongDirs, out string _))
            {
                hasDuplicateSubfolder = true;
                result.Add(Translation.Get(R.Messages.settingsProblem_songFolderIsSubfolderOfOtherSongFolder));
            }
        }

        return result;
    }

    public static List<Translation> GetRecordingSettingsProblems(Settings settings)
    {
        List<Translation> result = new();
        if (settings.MicProfiles.IsNullOrEmpty()
            || !settings.MicProfiles
                .AnyMatch(micProfile => micProfile.IsEnabled))
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_noMicProfiles));
        }
        else if (settings.MicProfiles
                 .AllMatch(micProfile => !micProfile.IsEnabled || !micProfile.IsConnected(ServerSideCompanionClientManager.Instance)))
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_noConnectedAndEnabledMicProfile));
        }
        return result;
    }

    public static List<Translation> GetPlayerSettingsProblems(Settings settings)
    {
        List<Translation> result = new();
        if (settings.PlayerProfiles.IsNullOrEmpty())
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_noPlayerProfile));
        }
        else if (!settings.PlayerProfiles.AnyMatch(playerProfile => playerProfile.IsEnabled))
        {
            result.Add(Translation.Get(R.Messages.settingsProblem_noEnabledPlayerProfile));
        }
        return result;
    }

    /**
     * Checks whether the path is already contained in the other folders, either directly or indirectly as subfolder.
     */
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

    public void SetProblems(List<Translation> settingsProblems)
    {
        bool oldHasIssues = this.hasIssues;
        hasIssues = !settingsProblems.IsNullOrEmpty();
        if (hasIssues
            && !oldHasIssues)
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
                 && oldHasIssues)
        {
            LeanTween.value(GameObjectUtils.FindAnyGameObject(), 1, 0, 0.3f)
                .setOnUpdate(value =>
                {
                    visualElement.style.scale = new Vector2(value, value);
                })
                .setEaseLinear();
        }

        tooltipControl.TooltipText = Translation.Of(settingsProblems.JoinWith("\n\n"));
    }
}
