using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFB;

public static class FileSystemDialogUtils
{
    public static ExtensionFilter[] CreateExtensionFilters(string filterName, params string[] extensions)
    {
        return new ExtensionFilter[] { new ExtensionFilter(filterName, extensions) };
    }

    public static ExtensionFilter[] CreateExtensionFilters(string filterName, IEnumerable<string> extensions)
    {
        return CreateExtensionFilters(filterName, extensions.ToArray());
    }

    public static void OpenFolderDialogToSetPath(
        string dialogTitle,
        string fallbackDirectory,
        Func<string> getter,
        Action<string> setter)
    {
        string oldValue = getter();
        string directory = DirectoryUtils.Exists(oldValue)
            ? oldValue
            : fallbackDirectory;
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }
        string selectedPath = OpenFolderDialog(dialogTitle, directory);
        if (!selectedPath.IsNullOrEmpty())
        {
            setter(selectedPath);
        }
    }

    public static void OpenFileDialogToSetPath(
        string dialogTitle,
        string fallbackDirectory,
        ExtensionFilter[] extensionFilters,
        Func<string> getter,
        Action<string> setter)
    {
        string oldValue = getter();
        string directory = FileUtils.Exists(oldValue)
            ? Path.GetDirectoryName(oldValue)
            : fallbackDirectory;
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }
        string selectedPath = OpenFileDialog(dialogTitle, directory, extensionFilters);
        if (!selectedPath.IsNullOrEmpty())
        {
            setter(selectedPath);
        }
    }

    public static string OpenFolderDialog(string title, string directory)
    {
#if UNITY_STANDALONE
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }

        string[] selectedPaths = StandaloneFileBrowser.OpenFolderPanel(title, directory, false);
        if (selectedPaths.IsNullOrEmpty()
            || !DirectoryUtils.Exists(selectedPaths.FirstOrDefault()))
        {
            return "";
        }

        return selectedPaths.FirstOrDefault()
            .Replace("\\", "/");
#else
        return "";
#endif
    }

    public static string OpenFileDialog(string title, string directory, ExtensionFilter[] extensionFilters)
    {
#if UNITY_STANDALONE
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }

        string[] selectedPaths = StandaloneFileBrowser.OpenFilePanel(title, directory, extensionFilters, false);
        if (selectedPaths.IsNullOrEmpty()
            || !FileUtils.Exists(selectedPaths.FirstOrDefault()))
        {
            return "";
        }

        return selectedPaths.FirstOrDefault()
            .Replace("\\", "/");
#else
        return "";
#endif
    }
}
