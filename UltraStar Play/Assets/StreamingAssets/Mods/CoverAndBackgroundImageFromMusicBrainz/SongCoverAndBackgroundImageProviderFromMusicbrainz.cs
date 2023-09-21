using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Flurl.Http;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class SongCoverImageFromMusicBrainzProvider : ISongCoverImageProvider, ISongBackgroundImageProvider
{
    private Dictionary<SongMeta, string> songMetaToCoverUriCache = new Dictionary<SongMeta, string>();

    public IObservable<string> GetCoverImageUri(SongMeta songMeta)
    {
        if (songMetaToCoverUriCache.TryGetValue(songMeta, out string cachedCoverUri))
        {
            return Observable.Return<string>(cachedCoverUri);
        }

        return ObservableUtils.RunOnNewTaskAsObservable(async () =>
            {
                string coverUri;
                try
                {
                    string releaseId = await GetMusicBrainzReleaseBySongArtistAndTitleAsync(songMeta.Artist, songMeta.Title);
                    coverUri = await GetMusicBrainzCoverUriAsync(releaseId);
                    Debug.Log($"Found cover URI for '{songMeta.Artist} - {songMeta.Title}': {coverUri}");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    coverUri = "";
                }

                songMetaToCoverUriCache[songMeta] = coverUri;
                return coverUri;
            },
            Disposable.Empty);
    }

    private async Task<string> GetMusicBrainzCoverUriAsync(string releaseId)
    {
        string escapedReleaseId = UnityWebRequest.EscapeURL(releaseId);
        string uri = $"https://coverartarchive.org/release/{escapedReleaseId}";

        CoverArtArchiveReleaseResponseJson json = await uri
            .WithHeader("User-Agent", "Some User Agent")
            .GetJsonAsync<CoverArtArchiveReleaseResponseJson>();

        if (json != null)
        {
            CoverArtArchiveReleaseImageJson imageJson = json.images
                .FirstOrDefault(it => it.front && !it.thumbnails.IsNullOrEmpty());

            if (imageJson != null)
            {
                // Prefer a small thumbnail image
                string coverUri;
                bool success = imageJson.thumbnails.TryGetValue("small", out coverUri);
                
                if (!success)
                {
                    success = imageJson.thumbnails.TryGetValue("large", out coverUri);
                }

                if (!success)
                {
                    // Elements are sorted from big to small. Take the smallest.
                    coverUri = imageJson.thumbnails.Values.LastOrDefault();
                }

                if (!coverUri.IsNullOrEmpty()
                    && (coverUri.StartsWith("http://")
                        || coverUri.StartsWith("https://")))
                {
                    return coverUri;
                }
            }
        }

        throw new Exception($"No front album art found for MusicBrainz release {releaseId}");
    }

    private async Task<string> GetMusicBrainzReleaseBySongArtistAndTitleAsync(
        string artist,
        string title)
    {
        string escapedTitle = UnityWebRequest.EscapeURL($"{title}");
        string uri = $"https://musicbrainz.org/ws/2/recording?query={escapedTitle}";

        Debug.Log($"Searching MusicBrainz release for '{artist} - {title}'. URI: '{uri}'");
        string xml = await uri
            .WithHeader("User-Agent", "Some User Agent")
            .GetStringAsync();

        Debug.Log($"MusicBrainz response: {xml}");

        // Select first release in the result
        XDocument xdocument = XDocument.Parse(xml);
        XElement xrelease = xdocument
            .Descendants()
            .FirstOrDefault(xelement => xelement.Name.LocalName == "release");

        Debug.Log($"MusicBrainz response xrelease: {xrelease}");
        if (xrelease != null)
        {
            // Grab the id of the release
            string releaseId = xrelease.Attribute("id")?.Value;
            Debug.Log($"MusicBrainz response xrelease id: {releaseId}");

            if (!releaseId.IsNullOrEmpty())
            {
                return releaseId;
            }
        }
        
        throw new Exception($"No MusicBrainz release found for artist '{artist}' and title '{title}'");
    }

    public IObservable<string> GetBackgroundImageUri(SongMeta songMeta)
    {
        return GetCoverImageUri(songMeta);
    }

    private class CoverArtArchiveReleaseResponseJson
    {
        public List<CoverArtArchiveReleaseImageJson> images;
    }

    private class CoverArtArchiveReleaseImageJson
    {
        public bool approved;
        public bool back;
        public bool front;
        public long id;
        public string image;
        public Dictionary<string, string> thumbnails;
    }
}
