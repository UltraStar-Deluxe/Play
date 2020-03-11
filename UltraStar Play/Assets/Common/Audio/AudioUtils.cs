using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NLayer;
using UniRx;

public static class AudioUtils
{
    // This method should only be called from tests and the AudioManager.
    // Use the cached version of the AudioManager for the normal game logic.
    public static AudioClip GetAudioClipUncached(string path, bool streamAudio)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Can not open audio file because it does not exist: {path}");
            return null;
        }
        string fileExtension = System.IO.Path.GetExtension(path);

        if (fileExtension.ToLowerInvariant().Equals(".mp3"))
        {
            AudioClip audioClip = LoadMp3(path);
            return audioClip;
        }
        else
        {
            return LoadAudio(path, streamAudio);
        }
    }

    private static AudioClip LoadAudio(string path, bool streamAudio)
    {
        using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
            downloadHandler.streamAudio = streamAudio;

            webRequest.SendWebRequest();
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError("Error Loading Audio: " + path);
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
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);
        MpegFile mpegFile = new MpegFile(path);
        if (mpegFile == null || mpegFile.Length < 1)
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