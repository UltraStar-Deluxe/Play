using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;

/**
 * When a song is missing its cover or background image
 * then this class can search in the song's folder for corresponding image files.
 */
public class SongMetaMissingImageProviderManager : AbstractSingletonBehaviour, INeedInjection
{
    public static SongMetaMissingImageProviderManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMetaMissingImageProviderManager>();

    private Dictionary<string, List<string>> directoryToImageFiles = new();

    private List<string> imageFileExtensionPatterns;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        imageFileExtensionPatterns = ApplicationUtils.supportedImageFiles
            .Select(fileExtension => "*." + fileExtension)
            .ToList();
    }

    private bool TryFindImagesFiles(string folder, out List<string> imageFiles)
    {
        if (!DirectoryUtils.Exists(folder))
        {
            imageFiles = null;
            return false;
        }

        if (!directoryToImageFiles.TryGetValue(folder, out imageFiles))
        {
            imageFiles = FileScannerUtils.ScanForFiles(new List<string>() { folder }, imageFileExtensionPatterns);
            directoryToImageFiles[folder] = imageFiles;
        }

        return !imageFiles.IsNullOrEmpty();
    }

    public static bool TryFindCoverImageInFolder(string folder, out string imageFile)
    {
        if (folder.IsNullOrEmpty()
            || !DirectoryUtils.Exists(folder))
        {
            imageFile = "";
            return false;
        }

        SongMetaMissingImageProviderManager instance = Instance;
        if (instance == null)
        {
            imageFile = "";
            return false;
        }

        return instance.DoTryFindCoverImageInFolder(folder, out imageFile);
    }

    public static bool TryFindBackgroundImageInFolder(string folder, out string imageFile)
    {
        if (folder.IsNullOrEmpty()
            || !DirectoryUtils.Exists(folder))
        {
            imageFile = "";
            return false;
        }

        SongMetaMissingImageProviderManager instance = Instance;
        if (instance == null)
        {
            imageFile = "";
            return false;
        }

        return instance.DoTryFindBackgroundImageInFolder(folder, out imageFile);
    }

    private bool DoTryFindCoverImageInFolder(string folder, out string imageFile)
    {
        if (!settings.SearchMissingCoverAndBackgroundImageInFolderOfSong
            || !TryFindImagesFiles(folder, out List<string> imageFiles))
        {
            imageFile = "";
            return false;
        }

        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "cover", "front", "album", "co" };
        imageFile = imageFiles
            .FirstOrDefault(imageFile =>
            {
                string fileName = Path.GetFileName(imageFile);
                return searchTerms.AnyMatch(searchTerm =>
                    fileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            })
            .OrIfNull(imageFiles.FirstOrDefault());
        return true;
    }

    private bool DoTryFindBackgroundImageInFolder(string folder, out string imageFile)
    {
        if (!settings.SearchMissingCoverAndBackgroundImageInFolderOfSong
            || !TryFindImagesFiles(folder, out List<string> imageFiles))
        {
            imageFile = "";
            return false;
        }

        // Prefer image files that have "background" or similar in their name.
        List<string> searchTerms = new List<string>() { "background", "back", "bg" };
        imageFile = imageFiles
            .FirstOrDefault(imageFile =>
            {
                string fileName = Path.GetFileName(imageFile);
                return searchTerms.AnyMatch(searchTerm =>
                    fileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            })
            .OrIfNull(imageFiles.FirstOrDefault());
        return true;
    }
}

