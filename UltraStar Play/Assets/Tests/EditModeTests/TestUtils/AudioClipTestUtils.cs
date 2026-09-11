using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class AudioClipTestUtils
{
    public static async Awaitable<AudioClip> LoadAudioClipAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Audio file not found", path);
        }

        string uri = "file://" + path;
        using UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN);
        await webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Failed to load AudioClip from {path}: {webRequest.error}");
        }

        return DownloadHandlerAudioClip.GetContent(webRequest);
    }
}
