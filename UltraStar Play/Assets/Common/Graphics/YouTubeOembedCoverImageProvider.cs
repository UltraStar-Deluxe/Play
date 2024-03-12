using System;
using System.Threading.Tasks;
using Flurl.Http;
using UniRx;

public class YouTubeOembedCoverImageProvider : ISongCoverImageProvider
{
    public IObservable<string> GetCoverImageUri(SongMeta songMeta)
    {
        if (TryGetYouTubeUri(songMeta.Website, out Uri uri))
        {
            return ObservableUtils.RunOnNewTaskAsObservable(async () => await GetCoverImageFromYouTube(uri));
        }

        return Observable.Empty<string>();
    }

    private static async Task<string> GetCoverImageFromYouTube(Uri uri)
    {
        string oembedUri = $"https://www.youtube.com/oembed?url={uri}&format=json";
        OembedResponse response = await oembedUri.GetJsonAsync<OembedResponse>();
        if (IsSupportedImageFileUri(response.thumbnail_url))
        {
            return response.thumbnail_url;
        }

        return "";
    }

    private static bool IsSupportedImageFileUri(string uri)
    {
        return uri.EndsWith(".jpg")
               || uri.EndsWith(".png");
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

    private class OembedResponse
    {
        public string thumbnail_url;
    }
}
