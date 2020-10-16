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

    // Do not use this method directly!
    // Instead, use the AudioManager where AudioClips are cached and released when no longer needed.
    public static IEnumerator LoadAudioClipFromUri(string uri, Action<AudioClip> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
        {
            DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
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

                Debug.LogError("Error loading AudioClip from: " + uri);
                Debug.LogError(webRequest.error);
                yield break;
            }

            onSuccess(downloadHandler.audioClip);
        }
    }

    public static IEnumerator LoadTextFromUri(string uri, Action<string> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
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

                Debug.LogError("Error loading text from: " + uri);
                Debug.LogError(webRequest.error);
                yield break;
            }

            onSuccess(webRequest.downloadHandler.text);
        }
    }
}
