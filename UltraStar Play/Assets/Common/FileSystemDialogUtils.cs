using System.Linq;
using SFB;

public static class FileSystemDialogUtils
{
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
