using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class UsdbVideoTagSyntaxMediaResolver : IAudioUriProvider, IVideoUriProvider, ICoverUriProvider, IBackgroundUriProvider
{
    public string GetAudioUri(SongMeta songMeta)
    {
        string mediaId = GetMediaId(songMeta.Video, "a")
            ?? GetMediaId(songMeta.Video, "v");
        if (mediaId.IsNullOrEmpty())
        {
            return null;
        }

        if (mediaId.StartsWith("http://") || mediaId.StartsWith("https://"))
        {
            return mediaId;
        }
        
        // Assume YouTube video.
        return $"https://www.youtube.com/watch?v={mediaId}";
    }

    public string GetVideoUri(SongMeta songMeta)
    {
        return GetAudioUri(songMeta);
    }

    public async Awaitable<string> GetCoverUriAsync(SongMeta songMeta)
    {
        string mediaId = GetMediaId(songMeta.Video, "co");
        if (mediaId.IsNullOrEmpty())
        {
            return null;
        }
        
        if (mediaId.StartsWith("http://") || mediaId.StartsWith("https://"))
        {
            return mediaId;
        }
        
        // Assume fanart.tv
        return $"https://assets.fanart.tv/fanart/{mediaId}";
    }

    public async Awaitable<string> GetBackgroundUriAsync(SongMeta songMeta)
    {
        string mediaId = GetMediaId(songMeta.Video, "bg");
        if (mediaId.IsNullOrEmpty())
        {
            return null;
        }
        
        if (!mediaId.IsNullOrEmpty() && mediaId.StartsWith("http://") || mediaId.StartsWith("https://"))
        {
            return mediaId;
        }
        
        // Assume fanart.tv
        return $"https://assets.fanart.tv/fanart/{mediaId}";
    }
    
    private static string GetMediaId(string usdbVideoTag, string tagId)
    {
        try
        {
            Match match = Regex.Match(usdbVideoTag, $"{tagId}=([^&,]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return null;
    }
}
