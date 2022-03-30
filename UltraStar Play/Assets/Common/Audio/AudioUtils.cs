using System;
using System.IO;
using System.Threading.Tasks;
using NLayer;
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

        string fileExtension = Path.GetExtension(uri);
        if (fileExtension.ToLowerInvariant().Equals(".mp3"))
        {
            if (WebRequestUtils.IsHttpOrHttpsUri(uri))
            {
                throw new ArgumentException("Streaming of MP3 audio is not supported. Please use OGG audio instead.");
            }

            string filePath = uri.Replace("file://", "");
            AudioClip audioClip = LoadMp3(filePath);
            return audioClip;
        }
        else
        {
            return LoadAudio(uri, streamAudio);
        }
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

    public static AudioClip LoadMp3(string path)
    {
        string filename = Path.GetFileNameWithoutExtension(path);
        MpegFile mpegFile = new MpegFile(path);
        if (mpegFile.Length < 1)
        {
            Debug.LogWarning($"Failed to load mp3 audio file: {path}");
            return null;
        }
        return AudioClip.Create(filename,
                                (int)(mpegFile.Length / sizeof(float) / mpegFile.Channels),
                                mpegFile.Channels,
                                mpegFile.SampleRate,
                                true,
                                data => OnReadMp3(data, mpegFile),
                                position => OnClipPositionSet(position, mpegFile));
    }

    private static void OnReadMp3(float[] data, MpegFile mpegFile)
    {
        mpegFile.ReadSamples(data, 0, data.Length);
    }

    private static void OnClipPositionSet(int position, MpegFile mpegFile)
    {
        mpegFile.Position = position * sizeof(float) * mpegFile.Channels;
    }
}
