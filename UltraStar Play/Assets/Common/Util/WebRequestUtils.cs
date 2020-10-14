using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequestUtils
{
    // Do not use this method directly!
    // Instead, use the ImageManager where Sprites and Textures are cached and released when no longer needed.
    public static IEnumerator LoadTexture2DFromUri(string uri, Action<Texture2D> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri))
        {
            DownloadHandlerTexture downloadHandler = webRequest.downloadHandler as DownloadHandlerTexture;
            webRequest.SendWebRequest();

            yield return new WaitUntil(() => webRequest.isDone);

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError("Error loading Texture2D from: " + uri);
                Debug.LogError(webRequest.error);
            }
            else
            {
                callback(downloadHandler.texture);
            }
        }
    }

    // Do not use this method directly!
    // Instead, use the AudioManager where AudioClips are cached and released when no longer needed.
    public static IEnumerator LoadAudioClipFromUri(string uri, Action<AudioClip> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
        {
            DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
            webRequest.SendWebRequest();

            yield return new WaitUntil(() => webRequest.isDone);

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError("Error loading AudioClip from: " + uri);
                Debug.LogError(webRequest.error);
            }
            else
            {
                callback(downloadHandler.audioClip);
            }
        }
    }

    public static IEnumerator LoadTextFromUri(string uri, Action<string> callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.SendWebRequest();

            yield return new WaitUntil(() => webRequest.isDone);

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError("Error loading text from: " + uri);
                Debug.LogError(webRequest.error);
            }
            else
            {
                callback(webRequest.downloadHandler.text);
            }
        }
    }
}
