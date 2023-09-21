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

    private readonly Dictionary<string, SongMeta> uriToSongMetaCache = new Dictionary<string, SongMeta>();

    private List<RemoteSongReference> remoteSongReferences = new List<RemoteSongReference>()
    {
        new RemoteSongReference()
        {
            Artist="Silver Note",
            Title = "Sonic Rainboom VIP",
            Uri ="https://raw.githubusercontent.com/UltraStar-Deluxe/songs-stream/main/Creative%20Commons/Silver%20Note%20-%20Sonic%20Rainboom%20VIP/song.txt",
        }
    };

    public IObservable<SongMeta> SearchSongs(SongSearchParameters searchParameters)
    {
        if (searchParameters == null
            || searchParameters.SearchText.IsNullOrEmpty())
        {
            return Observable.Empty<SongMeta>();
        }

        return ObservableUtils.RunOnNewTaskAsObservableElements(() => SearchSongListAsync(searchParameters.SearchText), Disposable.Empty);
    }

    public async Task<List<SongMeta>> SearchSongListAsync(string searchText)
    {
        if (searchText.IsNullOrEmpty())
        {
            return new List<SongMeta>();
        }

        List<RemoteSongReference> matchingRemoteSongReferences = remoteSongReferences
            .Where(it => StringContainsIgnoreCaseInvariantCulture(it.Artist, searchText)
                         || StringContainsIgnoreCaseInvariantCulture(it.Title, searchText))
            .ToList();
        if (matchingRemoteSongReferences.IsNullOrEmpty())
        {
            return new List<SongMeta>();
        }
        Debug.Log($"{nameof(OnlineSongRepositoryPrototype)} - Found {matchingRemoteSongReferences.Count} songs matching search '{searchText}'");
        
        List<SongMeta> songMetas = new List<SongMeta>();
        foreach (RemoteSongReference remoteSongReference in matchingRemoteSongReferences)
        {
            SongMeta songMeta = await LoadUltraStarSongFromUriAsync(remoteSongReference.Uri);
            if (songMeta != null)
            {
                songMetas.Add(songMeta);
            }
        }

        return songMetas;
    }

    private bool StringContainsIgnoreCaseInvariantCulture(string a, string b)
    {
        return a.ToLowerInvariant().Contains(b.ToLowerInvariant());
    }

    private async Task<SongMeta> LoadUltraStarSongFromUriAsync(string uri)
    {
        if (uriToSongMetaCache.TryGetValue(uri, out SongMeta cachedSongMeta))
        {
            return cachedSongMeta;
        }

        try
        {
            string ultraStarTxtContent = await uri
                .GetStringAsync();
            UltraStarSongMeta songMeta = UltraStarSongParser.ParseString(ultraStarTxtContent, out List<SongIssue> songIssues);
            songMeta.OnLoadVoices = () =>
            {
                List<Voice> voices = UltraStarSongVoicesParser.ParseString(ultraStarTxtContent, false);
                voices.ForEach(voice => songMeta.AddVoice(voice));
            };
            songMeta.RemoteSource = nameof(OnlineSongRepositoryPrototype);
            uriToSongMetaCache[uri] = songMeta;
            return songMeta;
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