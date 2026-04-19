using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
// StandaloneFileBrowser does not work with IL2CPP scripting backend on Windows. This is why usage is guarded with ENABLE_IL2CPP.
#if UNITY_STANDALONE && !ENABLE_IL2CPP
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }

        string[] selectedPaths = SFB.StandaloneFileBrowser.OpenFolderPanel(title, directory, false);
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
#if UNITY_STANDALONE && !ENABLE_IL2CPP
        if (!DirectoryUtils.Exists(directory))
        {
            directory = "";
        }

        string[] selectedPaths = SFB.StandaloneFileBrowser.OpenFilePanel(title, directory, ToSfbExtensionFilters(extensionFilters), false);
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

#if UNITY_STANDALONE && !ENABLE_IL2CPP
    private static SFB.ExtensionFilter[] ToSfbExtensionFilters(ExtensionFilter[] extensionFilters)
    {
        return extensionFilters
            .Select(filter => new SFB.ExtensionFilter(filter.Name, filter.Extensions))
            .ToArray();
    }
#endif
}

public struct ExtensionFilter
{
    public string Name { get; private set;  }
    public string[] Extensions { get; private set; }

    public ExtensionFilter(string name, string[] extensions)
    {
        Name = name;
        Extensions = extensions;
    }
    
    public ExtensionFilter(string name, string extension)
    {
        Name = name;
        Extensions = new[] { extension };
    }
}
