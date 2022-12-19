using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequestUtils
{
    // Do not use this method directly!
    // Instead, use the ImageManager where Sprites and Textures are cached and released when no longer needed.
    public static IEnumerator LoadTexture2DFromUri(string uri, Action<Texture2D> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(new Uri(uri));
        DownloadHandlerTexture downloadHandler = webRequest.downloadHandler as DownloadHandlerTexture;
        webRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            yield return null;
        }

        if (webRequest.result
            is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
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

    public static bool IsHttpOrHttpsUri(string uri)
    {
        return !uri.IsNullOrEmpty()
                && (uri.StartsWith("http://")
                    || uri.StartsWith("https://"));
    }

    public static bool IsNetworkPath(string absolutePath)
    {
        return !absolutePath.IsNullOrEmpty()
               && (absolutePath.StartsWith(@"\\")
                   || absolutePath.StartsWith("//"));
    }

    public static string AbsoluteFilePathToUri(string absolutePath)
    {
        string uri;
        if (absolutePath.StartsWith(@"\\"))
        {
            // This is a Windows-like network path.
            // MUST prefix it with the file:// scheme AND an additional slash for Unity API to work.
            // See https://forum.unity.com/threads/unitywebrequest-and-local-area-network.714353/
            return "file:///" + absolutePath;
        }

        if (absolutePath.StartsWith("//"))
        {
            // This also is a Unix-like network path. But because forward slashes are used, MUST prefix it with the file:// scheme ONLY for Unity API to work.
            return "file://" + absolutePath;
        }

        // This is a local path. MUST NOT prefix it with the file:// scheme.
        // Otherwise some paths may not work, e.g., when it contains a space AND a plus character.
        // See https://forum.unity.com/threads/unitywebrequest-file-protocol-not-working-with-plus-character-in-path-how-to-escape-the-uri.1364499/#post-8655012
        return absolutePath;
    }
}
