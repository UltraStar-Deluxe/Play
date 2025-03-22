using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;

public class FolderPreviewImageManager : AbstractSingletonBehaviour, INeedInjection
{
    public static FolderPreviewImageManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<FolderPreviewImageManager>();

    private readonly Dictionary<string, string> folderPathToPreviewImageUri = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    public string GetFolderPreviewImageUri(DirectoryInfo directoryInfo)
    {
        if (folderPathToPreviewImageUri.TryGetValue(directoryInfo.FullName, out string uri))
        {
            return uri;
        }

        using IDisposable d = new DisposableStopwatch($"GetFolderPreviewImageUri '{uri}'");
        uri = DoGetFolderPreviewImageUri(directoryInfo);

        folderPathToPreviewImageUri[directoryInfo.FullName] = uri;

        return uri;
    }

    private string DoGetFolderPreviewImageUri(DirectoryInfo directoryInfo)
    {
        List<string> imageFiles = FileScanner.GetFiles(directoryInfo.FullName,
            new FileScannerConfig(ApplicationUtils.supportedImageFiles.Select(it => $"*.{it}").ToList()) { Recursive = false });
        if (imageFiles.IsNullOrEmpty())
        {
            return null;
        }

        if (imageFiles.Count == 1)
        {
            return imageFiles.FirstOrDefault();
        }

        // Prefer file names that are typically used for album art
        List<string> preferredPatterns = new List<string>()
        {
            "cover", "preview", "album", "front"
        };
        return imageFiles
            .OrderBy(file =>
            {
                string fileNameLower = Path.GetFileName(file).ToLowerInvariant();
                return preferredPatterns.Any(pattern => fileNameLower.Contains(pattern)) ? 0 : 1;
            })
            .FirstOrDefault();
    }
}
