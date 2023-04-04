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
        
        string[] selectedFolders = StandaloneFileBrowser.OpenFolderPanel(title, directory, false);
        if (selectedFolders.IsNullOrEmpty()
            || !DirectoryUtils.Exists(selectedFolders.FirstOrDefault()))
        {
            return "";
        }

        return selectedFolders.FirstOrDefault()
            .Replace("\\", "/");
#else
        return "";
#endif
    }
}
