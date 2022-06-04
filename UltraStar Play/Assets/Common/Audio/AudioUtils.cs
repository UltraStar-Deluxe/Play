using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class AudioUtils
{
    // This method should only be called from tests and the AudioManager.
    // Use the cached version of the AudioManager for the normal game logic.
    public static AudioClip GetAudioClipUncached(string uri, bool streamAudio)
    {
        if (!WebRequestUtils.ResourceExists(uri))
        {
            Debug.LogWarning($"Audio file resource does not exist: {uri}");
            return null;
        }

        return LoadAudio(uri, streamAudio);
    }

    private static AudioClip LoadAudio(string uri, bool streamAudio)
    {
        using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
        {
            DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
            downloadHandler.streamAudio = streamAudio;

            webRequest.SendWebRequest();
            if (webRequest.result
                is UnityWebRequest.Result.ConnectionError
                or UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error Loading Audio: " + uri);
                Debug.LogError(webRequest.error);
                return null;
            }

            while (!webRequest.isDone)
            {
                Task.Delay(30);
            }
            return downloadHandler.audioClip;
        }
    }
}
