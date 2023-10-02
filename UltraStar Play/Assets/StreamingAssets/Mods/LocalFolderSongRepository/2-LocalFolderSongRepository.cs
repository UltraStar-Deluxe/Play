using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;

public class LocalFolderSongRepository : ISongRepository, IOnLoadMod
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

    private bool songScanStarted;
    private List<string> txtFilesInSongFolder = new List<string>();

    public void OnLoadMod()
    {
        SearchTxtFilesIfNotDoneYet();
    }

    public IObservable<SongRepositorySearchResultEntry> SearchSongs(SongRepositorySearchParameters searchParameters)
    {
        if (!DirectoryUtils.Exists(SongFolder)
            || searchParameters == null
            || searchParameters.SearchText.IsNullOrEmpty())
        {
            return Observable.Empty<SongRepositorySearchResultEntry>();
        }

        SearchTxtFilesIfNotDoneYet();

        return ObservableUtils.RunOnNewTaskAsObservableElements(() => SearchSongList(searchParameters), Disposable.Empty);
    }

    private void SearchTxtFilesIfNotDoneYet()
    {
        if (songScanStarted
            || !DirectoryUtils.Exists(SongFolder))
        {
            return;
        }

        songScanStarted = true;
        Task.Run(() => DoSearchTxtFiles());
    }
    
    private void DoSearchTxtFiles()
    {
        Debug.Log($"Searching for txt files in '{SongFolder}'");
        txtFilesInSongFolder = DirectoryUtils.GetFiles(SongFolder, true, $"*.txt");
        Debug.Log($"Found {txtFilesInSongFolder.Count} txt files in '{SongFolder}'");
    }

    public List<SongRepositorySearchResultEntry> SearchSongList(SongRepositorySearchParameters searchParameters)
    {
        string searchText = searchParameters.SearchText;
        if (searchText.IsNullOrEmpty()
            || !DirectoryUtils.Exists(SongFolder))
        {
            return new List<SongRepositorySearchResultEntry>();
        }

        string searchTextLower = searchText.ToLower();
        List<SongRepositorySearchResultEntry> resultEntries = txtFilesInSongFolder
            .Where(txtFile =>
            {
                string fileNameLower = Path.GetFileName(txtFile).ToLowerInvariant();
                return fileNameLower.Contains(searchTextLower);
            })
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
            Encoding encoding = GetEncodingFromModSettings();
            SongMeta songMeta = UltraStarSongParser.ParseFile(txtFile, out List<SongIssue> songIssues, encoding);
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

    private Encoding GetEncodingFromModSettings()
    {
        try
        {
            return !modSettings.encodingName.IsNullOrEmpty()
                ? Encoding.GetEncoding(modSettings.encodingName)
                : null;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to get encoding from mod settings: {ex.Message}, guessing encoding instead.");
            return null;
        }
    }
}