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

    private SongMeta LoadUltraStarSongFromFile(string txtFile)
    {
        if (txtFileToSongMetaCache.TryGetValue(txtFile, out SongMeta cachedSongMeta))
        {
            return cachedSongMeta;
        }

        try
        {
            SongMeta songMeta = UltraStarSongParser.ParseFile(txtFile, out List<SongIssue> songIssues);
            songMeta.RemoteSource = nameof(LocalFolderSongRepository);
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