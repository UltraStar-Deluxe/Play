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

    private readonly Dictionary<string, SongRepositorySearchResultEntry> txtFileToSearchResultCache = new Dictionary<string, SongRepositorySearchResultEntry>();

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

    public IObservable<SongRepositorySearchResultEntry> SearchSongs(SongRepositorySearchParameters searchParameters)
    {
        if (searchParameters == null
            || searchParameters.SearchText.IsNullOrEmpty())
        {
            return Observable.Empty<SongRepositorySearchResultEntry>();
        }

        return ObservableUtils.RunOnNewTaskAsObservableElements(() => SearchSongList(searchParameters), Disposable.Empty);
    }

    public List<SongRepositorySearchResultEntry> SearchSongList(SongRepositorySearchParameters searchParameters)
    {
        string searchText = searchParameters.SearchText;
        if (searchText.IsNullOrEmpty()
            || !DirectoryUtils.Exists(SongFolder))
        {
            return new List<SongRepositorySearchResultEntry>();
        }

        List<string> txtFiles = DirectoryUtils.GetFiles(SongFolder, true, $"*{searchText}*.txt");

        List<SongRepositorySearchResultEntry> resultEntries = txtFiles
            .Select(txtFile => LoadUltraStarSongFromFile(txtFile))
            .Where(it => it != null)
            .ToList();        
        Debug.Log($"{nameof(LocalFolderSongRepository)} - Found {resultEntries.Count} songs matching search '{searchText}'");
        return resultEntries;
    }

    private SongRepositorySearchResultEntry LoadUltraStarSongFromFile(string txtFile)
    {
        if (txtFileToSearchResultCache.TryGetValue(txtFile, out SongRepositorySearchResultEntry cachedResultEntry))
        {
            return cachedResultEntry;
        }

        try
        {
            SongMeta songMeta = UltraStarSongParser.ParseFile(txtFile, out List<SongIssue> songIssues);
            SongRepositorySearchResultEntry resultEntry = new SongRepositorySearchResultEntry(songMeta, songIssues);
            songMeta.RemoteSource = nameof(LocalFolderSongRepository);
            txtFileToSearchResultCache[txtFile] = resultEntry;
            return resultEntry;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"{nameof(LocalFolderSongRepository)} - Failed to load UltraStar song file '{txtFile}': {ex.Message}");
            return null;
        }
    }
}