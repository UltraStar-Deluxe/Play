using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class UsdbVideoTagSyntaxMediaResolver : IAudioUriProvider, ICoverUriProvider, IBackgroundUriProvider
{
    // Only implement GetAudioUri because the audio is more relevant to play the karaoke song.
    // That it also includes the video is a detail. 
    public string GetAudioUri(SongMeta songMeta)
    {
        string audioPart = TryGetMediaId(songMeta.Video, "a")
            ?? TryGetMediaId(songMeta.Video, "v");
        if (audioPart.IsNullOrEmpty())
        {
            return null;
        }

        if (audioPart.StartsWith("http://") || audioPart.StartsWith("https://"))
        {
            return audioPart;
        }
        
        // Assume YouTube video.
        return $"https://www.youtube.com/watch?v={audioPart}";
    }

    public async Awaitable<string> GetCoverUriAsync(SongMeta songMeta)
    {
        string coverPart = TryGetMediaId(songMeta.Video, "co");
        return $"https://assets.fanart.tv/fanart/{coverPart}";
    }

    public async Awaitable<string> GetBackgroundUriAsync(SongMeta songMeta)
    {
        string backgroundPart = TryGetMediaId(songMeta.Video, "bg");
        return $"https://assets.fanart.tv/fanart/{backgroundPart}";
    }
    
    private static string TryGetMediaId(string usdbVideoTag, string tagId)
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
