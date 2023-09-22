using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using UniInject;
using UniRx;
using UnityEngine;

public class OnlineSongRepositoryPrototype : ISongRepository
{
    [Inject]
    private OnlineSongRepositoryPrototypeModSettings modSettings;

    private readonly Dictionary<string, SongRepositorySearchResultEntry> uriToSearchResultCache = new Dictionary<string, SongRepositorySearchResultEntry>();

    private List<RemoteSongReference> remoteSongReferences = new List<RemoteSongReference>()
    {
        new RemoteSongReference()
        {
            Artist="Silver Note",
            Title = "Sonic Rainboom VIP",
            Uri ="https://raw.githubusercontent.com/UltraStar-Deluxe/songs-stream/main/Creative%20Commons/Silver%20Note%20-%20Sonic%20Rainboom%20VIP/song.txt",
        }
    };

    public IObservable<SongRepositorySearchResultEntry> SearchSongs(SongRepositorySearchParameters searchParameters)
    {
        if (searchParameters == null
            || searchParameters.SearchText.IsNullOrEmpty())
        {
            return Observable.Empty<SongRepositorySearchResultEntry>();
        }

        return ObservableUtils.RunOnNewTaskAsObservableElements(() => SearchSongListAsync(searchParameters), Disposable.Empty);
    }

    public async Task<List<SongRepositorySearchResultEntry>> SearchSongListAsync(SongRepositorySearchParameters searchParameters)
    {
        string searchText = searchParameters.SearchText;
        if (searchText.IsNullOrEmpty())
        {
            return new List<SongRepositorySearchResultEntry>();
        }

        List<RemoteSongReference> matchingRemoteSongReferences = remoteSongReferences
            .Where(it => StringContainsIgnoreCaseInvariantCulture(it.Artist, searchText)
                         || StringContainsIgnoreCaseInvariantCulture(it.Title, searchText))
            .ToList();
        if (matchingRemoteSongReferences.IsNullOrEmpty())
        {
            return new List<SongRepositorySearchResultEntry>();
        }
        Debug.Log($"{nameof(OnlineSongRepositoryPrototype)} - Found {matchingRemoteSongReferences.Count} songs matching search '{searchText}'");
        
        List<SongRepositorySearchResultEntry> resultEntries = new List<SongRepositorySearchResultEntry>();
        foreach (RemoteSongReference remoteSongReference in matchingRemoteSongReferences)
        {
            SongRepositorySearchResultEntry resultEntry = await LoadUltraStarSongFromUriAsync(remoteSongReference.Uri);
            if (resultEntry != null)
            {
                resultEntries.Add(resultEntry);
            }
        }

        return resultEntries;
    }

    private bool StringContainsIgnoreCaseInvariantCulture(string a, string b)
    {
        return a.ToLowerInvariant().Contains(b.ToLowerInvariant());
    }

    private async Task<SongRepositorySearchResultEntry> LoadUltraStarSongFromUriAsync(string uri)
    {
        if (uriToSearchResultCache.TryGetValue(uri, out SongRepositorySearchResultEntry cachedSearchResultEntry))
        {
            return cachedSearchResultEntry;
        }

        try
        {
            string ultraStarTxtContent = await uri
                .WithHeader("User-Agent", "Some User Agent")
                .GetStringAsync();
            UltraStarSongMeta songMeta = UltraStarSongParser.ParseString(ultraStarTxtContent, out List<SongIssue> songIssues);
            songMeta.RemoteSource = nameof(OnlineSongRepositoryPrototype);
            SongRepositorySearchResultEntry resultEntry = new SongRepositorySearchResultEntry(songMeta, songIssues);
            uriToSearchResultCache[uri] = resultEntry;
            return resultEntry;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"{nameof(OnlineSongRepositoryPrototype)} - Failed to load UltraStar song from URI '{uri}': {ex.Message}");
            return null;
        }
    }

    public class RemoteSongReference
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Uri { get; set; }
    }
}