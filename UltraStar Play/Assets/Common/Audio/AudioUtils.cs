using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using NLayer;
using UniRx;

public static class AudioUtils
{
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