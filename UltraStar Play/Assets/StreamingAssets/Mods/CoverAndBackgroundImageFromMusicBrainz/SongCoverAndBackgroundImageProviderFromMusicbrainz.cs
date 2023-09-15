using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Flurl.Http;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class SongCoverImageFromMusicBrainzProvider : ISongCoverImageProvider, ISongBackgroundImageProvider
{
    private Dictionary<SongMeta, string> songMetaToCoverUri = new Dictionary<SongMeta, string>();

    public IObservable<string> GetCoverImageUri(SongMeta songMeta)
    {
        if (songMetaToCoverUri.TryGetValue(songMeta, out string cachedUri))
        {
            if (cachedUri.IsNullOrEmpty())
            {
                return Observable.Empty<string>();
            }
            else
            {
                return Observable.Return<string>(cachedUri);
            }
        }

        return GetMusicBrainzReleaseBySongArtistAndTitle(songMeta.Artist, songMeta.Title)
            .SelectMany(releaseId => GetMusicBrainzCoverUri(releaseId))
            .SelectMany(coverUri => 
            {
                Debug.Log($"Found cover URI for '{songMeta.Artist} - {songMeta.Title}': {coverUri}");
                
                // Cache value for following calls
                songMetaToCoverUri[songMeta] = coverUri;

                return Observable.Return<string>(coverUri);
            })
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                songMetaToCoverUri[songMeta] = "";
            });
    }

    private IObservable<string> GetMusicBrainzCoverUri(string releaseId)
    {
        return ObservableUtils.RunOnNewTaskAsObservable(async () =>
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
        },
        Disposable.Empty);
    }

    private IObservable<string> GetMusicBrainzReleaseBySongArtistAndTitle(
        string artist,
        string title)
    {
        return ObservableUtils.RunOnNewTaskAsObservable(async () =>
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
                .FirstOrDefault(xelmeent => xelmeent.Name.LocalName == "release");

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
        },
        Disposable.Empty);
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
