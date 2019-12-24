using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NLayer;

public static class AudioUtils
{
    public static AudioClip GetAudioClip(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Can not open audio file because it does not exist: {path}");
            return null;
        }
        string fileExtension = System.IO.Path.GetExtension(path);

        using (new DisposableStopwatch("Loaded audio in <millis> ms"))
        {
            if (fileExtension.ToLowerInvariant().Equals(".mp3"))
            {
                return LoadMp3(path);
            }
            else
            {
                return LoadAudio(path);
            }
        }
    }

    private static AudioClip LoadAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            www.SendWebRequest();

            if (www.isNetworkError)
            {
                Debug.LogError("Error Loading Audio: " + path);
                Debug.LogError(www.error);
                return null;
            }
            else
            {
                while (!www.isDone)
                {
                    Task.Delay(30);
                }
                return DownloadHandlerAudioClip.GetContent(www);
            }
        }
    }

    private static AudioClip LoadMp3(string path)
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