using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class LocalFolderSongRepository : ISongRepository
{
    [Inject]
    private LocalFolderSongRepositoryModSettings modSettings;

    private string SongFolder => modSettings.songFolder;

    private readonly Dictionary<string, SongMeta> txtFileToSongMetaCache = new Dictionary<string, SongMeta>();

    private FileScanner txtFileScanner;
    private FileScanner TxtFileScanner
    {
        get
        {
            if (txtFileScanner == null)
            {
                txtFileScanner = new FileScanner("*.txt", true, true);
            }
            return txtFileScanner;
        }
    }

    public IObservable<SongMeta> SearchSongs(SongSearchParameters searchParameters)
    {
        if (searchParameters == null
            || searchParameters.SearchText.IsNullOrEmpty())
        {
            return Observable.Empty<SongMeta>();
        }

        return ObservableUtils.RunOnNewTaskAsObservableElements(() => SearchSongList(searchParameters.SearchText), Disposable.Empty);
    }

    public List<SongMeta> SearchSongList(string searchText)
    {
        if (searchText.IsNullOrEmpty()
            || !DirectoryUtils.Exists(SongFolder))
        {
            return new List<SongMeta>();
        }

        List<string> txtFiles = DirectoryUtils.GetFiles(SongFolder, true, $"*{searchText}*.txt");

        List<SongMeta> songMetas = txtFiles
            .Select(txtFile => LoadUltraStarSongFromFile(txtFile))
            .Where(songMeta => songMeta != null)
            .ToList();        
        Debug.Log($"{nameof(LocalFolderSongRepository)} - Found {songMetas.Count} songs matching search '{searchText}'");
        return songMetas;
    }

    // public IObservable<SongMeta> GetDefaultSongs()
    // {
    //     return ObservableUtils.RunOnNewTaskAsObservableElements(GetDefaultSongList, Disposable.Empty);
    // }

    // public List<SongMeta> GetDefaultSongList()
    // {
    //     if (!DirectoryUtils.Exists(SongFolder))
    //     {
    //         return new List<SongMeta>();
    //     }

    //     List<string> txtFiles = new List<string>();
    //     TryAddTxtFilesUntilCount(txtFiles, SongFolder, 10, subFolders => subFolders);

    //     List<SongMeta> songMetas = txtFiles
    //         .Select(txtFile => LoadUltraStarSongFromFile(txtFile))
    //         .Where(songMeta => songMeta != null)
    //         .ToList();

    //     return songMetas;
    // }

    // public IObservable<SongMeta> GetRandomSongs()
    // {
    //     return ObservableUtils.RunOnNewTaskAsObservableElements(GetRandomSongList, Disposable.Empty);
    // }

    // public List<SongMeta> GetRandomSongList()
    // {
    //     if (!DirectoryUtils.Exists(SongFolder))
    //     {
    //         return new List<SongMeta>();
    //     }

    //     List<string> txtFiles = new List<string>();
    //     TryAddTxtFilesUntilCount(txtFiles, SongFolder, 10, subFolders => ShuffleList(subFolders));

    //     return txtFiles
    //         .Select(txtFile => LoadUltraStarSongFromFile(txtFile))
    //         .Where(songMeta => songMeta != null)
    //         .ToList();
    // }

    // private bool TryAddTxtFilesUntilCount(List<string> txtFiles, string folder, int targetFileCount, Func<List<string>, List<string>> subFolderSelector)
    // {
    //     if (txtFiles.Count >= targetFileCount)
    //     {
    //         return true;
    //     }

    //     if (folder.IsNullOrEmpty())
    //     {
    //         return false;
    //     }

    //     List<string> txtFilesInFolder = txtFileScanner.GetFiles(folder, false);
    //     if (TryAddUntilCount(txtFiles, txtFilesInFolder, targetFileCount))
    //     {
    //         return true;
    //     }

    //     List<string> subFolders = DirectoryUtils.GetDirectories(folder, false, "*.txt");
    //     List<string> subFolderSelection = subFolderSelector(subFolders);
    //     foreach (string subFolder in subFolderSelection)
    //     {
    //         if (TryAddTxtFilesUntilCount(txtFiles, subFolder, targetFileCount, subFolderSelector))
    //         {
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    // private bool TryAddUntilCount<T>(List<T> targetList, List<T> sourceList, int targetCount)
    // {
    //     if (targetList.Count >= targetCount)
    //     {
    //         return true;
    //     }

    //     for (int i = 0; i < sourceList.Count; i++)
    //     {
    //         targetList.Add(sourceList[i]);
    //         if (targetList.Count >= targetCount)
    //         {
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    // private List<T> ShuffleList<T>(List<T> list)
    // {
    //     List<T> shuffled = new List<T>(list);

    //     int remainingElementCount = shuffled.Count;
    //     while (remainingElementCount > 1) 
    //     {
    //         int k = UnityEngine.Random.Range(0, remainingElementCount);
    //         T temp = shuffled[remainingElementCount];
    //         shuffled[remainingElementCount] = shuffled[k];
    //         shuffled[k] = temp;
    //         remainingElementCount--;
    //     }
    //     return shuffled;
    // }

    private SongMeta LoadUltraStarSongFromFile(string txtFile)
    {
        if (txtFileToSongMetaCache.TryGetValue(txtFile, out SongMeta cachedSongMeta))
        {
            return cachedSongMeta;
        }

        try
        {
            SongMeta songMeta = UltraStarSongParser.ParseFile(txtFile, out List<SongIssue> songIssues, null, true);
            txtFileToSongMetaCache[txtFile] = songMeta;
            return songMeta;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"{nameof(LocalFolderSongRepository)} - Failed to load UltraStar song file '{txtFile}': {ex.Message}");
            return null;
        }
    }
}