using System;
using System.Collections.Generic;
using System.Linq;

public class SongCoverAndBackgroundImageProviderFromFolder : ISongCoverImageProvider, ISongBackgroundImageProvider
{
    private static readonly List<string> imageFileExtensionPatterns = new List<string>()
    {
        "*.png",
        "*.jpg",
        "*.jpeg",
    };

    public IObservable<string> GetCoverImageUri(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "cover", "front", "album", "co" };
        return GetCoverImageUriPreferSearchTerms(songMeta, searchTerms);
    }

    public IObservable<string> GetBackgroundImageUri(SongMeta songMeta)
    {
        // Prefer image files that have "cover" or similar in their name.
        List<string> searchTerms = new List<string>() { "background", "back", "bg" };
        return GetCoverImageUriPreferSearchTerms(songMeta, searchTerms);
    }

    private IObservable<string> GetCoverImageUriPreferSearchTerms(SongMeta songMeta, List<string> searchTerms)
    {
        List<string> imageFiles = GetImageFilesInFolder(songMeta);
        if (imageFiles.IsNullOrEmpty())
        {
            return UniRx.Observable.Empty<string>();
        }

        string finalImageFile = Enumerable.FirstOrDefault(imageFiles, file =>
            {
                string fileName = PathUtils.GetFileName(file);
                return searchTerms.AnyMatch(searchTerm => fileName.ToLowerInvariant().Contains(searchTerm));
            })
            .OrIfNull(Enumerable.FirstOrDefault(imageFiles));

        if (finalImageFile.IsNullOrEmpty())
        {
            return UniRx.Observable.Empty<string>();
        }

         return UniRx.Observable.Return<string>(finalImageFile);
    }

    private List<string> GetImageFilesInFolder(SongMeta songMeta)
    {
        if (songMeta == null
            || !DirectoryUtils.Exists(songMeta.Directory))
        {
            return new List<string>();
        }

        return FileScannerUtils.ScanForFiles(new List<string>() { songMeta.Directory }, imageFileExtensionPatterns);
    }
}