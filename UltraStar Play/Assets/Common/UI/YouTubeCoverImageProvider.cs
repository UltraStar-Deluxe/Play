using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class YouTubeCoverImageProvider : ISongCoverImageProvider
{
    public async Awaitable<string> GetCoverImageUriAsync(SongMeta songMeta)
    {
        string webViewUri = SongMetaUtils.GetWebViewUrl(songMeta);
        if (TryGetYouTubeUri(webViewUri, out Uri uri))
        {
            return GetCoverImageFromYouTube(uri);
        }

        return "";
    }

    private static string GetCoverImageFromYouTube(Uri uri)
    {
        // https://stackoverflow.com/questions/2068344/how-do-i-get-a-youtube-video-thumbnail-from-the-youtube-api
        string videoId = GetYouTubeVideoId(uri);
        if (videoId.IsNullOrEmpty())
        {
            return "";
        }
        return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
    }

    private static string GetYouTubeVideoId(Uri uri)
    {
        Dictionary<string,string> queryParameters = ParseQueryString(uri);
        if (queryParameters.TryGetValue("v", out string videoId))
        {
            return videoId;
        }
        return "";
    }

    private static bool TryGetYouTubeUri(string resource, out Uri uri)
    {
        if (resource.IsNullOrEmpty()
            || !WebRequestUtils.IsHttpOrHttpsUri(resource))
        {
            uri = null;
            return false;
        }

        try
        {
            uri = new Uri(resource);
            if (uri.Host.Contains("youtube"))
            {
                return true;
            }

            uri = null;
            return false;
        }
        catch (UriFormatException)
        {
            uri = null;
            return false;
        }
    }

    /**
     * Parse a URI query string.
     * Workaround because System.Web.HttpUtility.ParseQueryString
     * is not compiled into the project by Unity by default.
     */
    public static Dictionary<string, string> ParseQueryString(Uri uri)
    {
        string queryString = uri.Query;

        Dictionary<string, string> parsedParameters = new Dictionary<string, string>();
        if (queryString.IsNullOrEmpty())
        {
            return parsedParameters;
        }

        try
        {
            string[] queryParams = queryString.TrimStart('?').Split('&');
            foreach (string param in queryParams)
            {
                string[] keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = Uri.UnescapeDataString(keyValue[0]);
                    string value = Uri.UnescapeDataString(keyValue[1]);
                    parsedParameters[key] = value;
                }
                else if (keyValue.Length == 1)
                {
                    string key = Uri.UnescapeDataString(keyValue[0]);
                    parsedParameters[key] = null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to parse query string of URI '{uri}': {ex.Message}");
            return parsedParameters;
        }

        return parsedParameters;
    }
}
