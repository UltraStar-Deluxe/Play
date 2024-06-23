using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

public class SongCoverAndBackgroundImageProviderFromFolder : ISongCoverImageProvider, ISongBackgroundImageProvider
{
    private static readonly List<string> imageFileExtensionPatterns = new List<string>()
    {
        "*.png",
        "*.jpg",
        "*.jpeg",
    };
    
    // Cache of previously found values
    private Dictionary<string, string> folderToCoverUri = new Dictionary<string, string>();
    private Dictionary<string, string> folderToBackgroundUri = new Dictionary<string, string>();

    public IObservable<string> GetCoverImageUri(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "cover", "front", "album", "co" };
        return GetImageUriPreferSearchTerms(songMeta, searchTerms, folderToCoverUri);
    }

    public IObservable<string> GetBackgroundImageUri(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "background", "back", "bg" };
        return GetImageUriPreferSearchTerms(songMeta, searchTerms, folderToBackgroundUri);
    }

    private IObservable<string> GetImageUriPreferSearchTerms(SongMeta songMeta, List<string> searchTerms, Dictionary<string, string> folderToUri)
    {
        string directoryPath = SongMetaUtils.GetDirectoryPath(songMeta);
        if (songMeta == null
            || !DirectoryUtils.Exists(directoryPath))
        {
            return Observable.Empty<string>();
        }

        if (folderToUri.TryGetValue(directoryPath, out string uri))
        {
            if (uri.IsNullOrEmpty())
            {
                return Observable.Empty<string>();
            }
            else
            {
                return Observable.Return<string>(uri);
            }
        }

        List<string> imageFiles = FileScannerUtils.ScanForFiles(new List<string>() { directoryPath }, imageFileExtensionPatterns);
        if (imageFiles.IsNullOrEmpty())
        {
            // Cache the value for following calls.
            folderToUri[directoryPath] = "";
            return Observable.Empty<string>();
        }

        string finalImageFile = imageFiles.FirstOrDefault(file =>
            {
                string fileName = PathUtils.GetFileName(file);
                return searchTerms.AnyMatch(searchTerm => fileName.ToLowerInvariant().Contains(searchTerm));
            })
            .OrIfNull(imageFiles.FirstOrDefault());

        // Cache the value for following calls.
        folderToUri[directoryPath] = finalImageFile;

        if (finalImageFile.IsNullOrEmpty())
        {
            return Observable.Empty<string>();
        }

         return Observable.Return<string>(finalImageFile);
    }
}