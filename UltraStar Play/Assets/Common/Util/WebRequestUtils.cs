using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequestUtils
{
    // Do not use this method directly!
    // Instead, use the ImageManager where Sprites and Textures are cached and released when no longer needed.
    public static IEnumerator LoadTexture2DFromUri(string uri, Action<Texture2D> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri))
        {
            DownloadHandlerTexture downloadHandler = webRequest.downloadHandler as DownloadHandlerTexture;
            webRequest.SendWebRequest();

            while (!webRequest.isDone)
            {
                yield return null;
            }

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                if (onFailure != null)
                {
                    onFailure(webRequest);
                    yield break;
                }

                Debug.LogError("Error loading Texture2D from: " + uri);
                Debug.LogError(webRequest.error);
                yield break;
            }

            onSuccess(downloadHandler.texture);
        }
    }

    public static bool IsHttpOrHttpsUri(string uri)
    {
        return uri.StartsWith("http://")
               || uri.StartsWith("https://");
    }

    public static bool ResourceExists(string uri)
    {
        if (!uri.IsNullOrEmpty()
            && uri.StartsWith("file://")
            && !File.Exists(uri.Replace("file://", "")))
        {
            return false;
        }

        return !uri.IsNullOrEmpty();
    }
}
