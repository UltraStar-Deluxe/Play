using System;
using UnityEngine;

/**
 * Workaround for inaccurate mp3 files in Unity: load all samples upfront instead of streaming the audio.
 * Disadvantage: Unity loads the audio in blocking way when streamAudio is false, which results in UI stutter.
 * See https://github.com/UltraStar-Deluxe/Play/issues/504
 */
// TODO: Remove when fixed by Unity
public class InaccurateMp3WorkaroundUtils
{
    public static bool ShouldStreamAudio(string audioUri)
    {
        if (audioUri.IsNullOrEmpty())
        {
            return true;
        }

        bool streamAudio = !audioUri.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase);
        if (!streamAudio)
        {
            Debug.Log("Not streaming audio as workaround for Unity's inaccurate mp3");
        }

        return streamAudio;
    }
}
