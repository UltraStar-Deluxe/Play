using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

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

    public async Awaitable<string> GetCoverImageUriAsync(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "cover", "front", "album", "co" };
        return GetImageUriPreferSearchTermsAsync(songMeta, searchTerms, folderToCoverUri);
    }

    public async Awaitable<string> GetBackgroundImageUriAsync(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "background", "back", "bg" };
        return GetImageUriPreferSearchTermsAsync(songMeta, searchTerms, folderToBackgroundUri);
    }

    private string GetImageUriPreferSearchTermsAsync(SongMeta songMeta, List<string> searchTerms, Dictionary<string, string> folderToUri)
    {
        string directoryPath = SongMetaUtils.GetDirectoryPath(songMeta);
        if (songMeta == null
            || !DirectoryUtils.Exists(directoryPath))
        {
            return "";
        }

        if (folderToUri.TryGetValue(directoryPath, out string uri))
        {
            if (uri.IsNullOrEmpty())
            {
                return "";
            }
            else
            {
                return uri;
            }
        }

        List<string> imageFiles = FileScanner.GetFiles(directoryPath, new FileScannerConfig(imageFileExtensionPatterns) { Recursive = true } );
        if (imageFiles.IsNullOrEmpty())
        {
            // Cache the value for following calls.
            folderToUri[directoryPath] = "";
            return "";
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
            return "";
        }

         return finalImageFile;
    }
}