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
        return LoadAudio(uri, streamAudio);
    }

    private static AudioClip LoadAudio(string uri, bool streamAudio)
    {
        Uri uriHandle = new Uri(uri);
        using UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uriHandle, AudioType.UNKNOWN);
        DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
        downloadHandler.streamAudio = streamAudio;

        webRequest.SendWebRequest();
        while (!webRequest.isDone)
        {
            Task.Delay(30);
        }

        if (webRequest.result
            is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error Loading Audio: " + uri);
            Debug.LogError(webRequest.error);
            return null;
        }

        AudioClip audioClip = downloadHandler.audioClip;
        string fileName = Path.GetFileName(uriHandle.LocalPath);
        audioClip.name = $"Audio file '{fileName}'";
        return audioClip;
    }

    public static float[] ToMonoAudioSamples(float[] originalSamples, int channelCount)
    {
        if (channelCount <= 1)
        {
            return originalSamples;
        }

        // Stereo to mono => take the average of the channels
        float[] monoSamples = new float[originalSamples.Length / channelCount];
        int monoSampleIndex = 0;
        for (int stereoSampleIndex = 0; stereoSampleIndex < originalSamples.Length && monoSampleIndex < monoSamples.Length; stereoSampleIndex += channelCount)
        {
            float sampleSum = 0;
            for (int channelIndex = 0; channelIndex < channelCount && (stereoSampleIndex + channelIndex) < originalSamples.Length; channelIndex++)
            {
                sampleSum += originalSamples[stereoSampleIndex + channelIndex];
            }

            float sampleAverage = sampleSum / channelCount;
            monoSamples[monoSampleIndex] = sampleAverage;
            monoSampleIndex++;
        }

        return monoSamples;
    }

    public static short[] ToShortSampleArray(float[] floatSampleArray, int startIndex, int endIndex)
    {
        int lengthInSamples = endIndex - startIndex;
        short[] shortSampleArray = new short[lengthInSamples];
        for (int i = 0; i < lengthInSamples; i++)
        {
            shortSampleArray[i] = (short)Math.Floor(floatSampleArray[i + startIndex] * short.MaxValue);
        }

        return shortSampleArray;
    }

    public static float[] GetAudioSamples(double startInMillis, double lengthInMillis, AudioClip audioClip, bool convertToMono)
    {
        if (lengthInMillis <= 0)
        {
            return null;
        }

        int samplesPerSecondMono = audioClip.frequency;
        int samplesPerSecond = samplesPerSecondMono * audioClip.channels;
        int maxSample = audioClip.samples * audioClip.channels;
        double lengthInSamplesStereo = lengthInMillis / 1000.0 * samplesPerSecond;

        float[] samplesStereo = new float[(int)lengthInSamplesStereo];

        int startInSamplesMono = (int) (startInMillis / 1000.0 * samplesPerSecondMono);
        startInSamplesMono = NumberUtils.Limit(startInSamplesMono, 0, maxSample);
        // Note that GetData always takes the offset in MONO samples, even if there are more channels.
        audioClip.GetData(samplesStereo, startInSamplesMono);

        // WavFileWriter.WriteFile(Application.persistentDataPath + "/samples-stereo.wav", audioClip.frequency, audioClip.channels, samplesStereo);

        if (convertToMono)
        {
            float[] samplesMono = ToMonoAudioSamples(samplesStereo, audioClip.channels);
            // WavFileWriter.WriteFile(Application.persistentDataPath + "/samples-mono.wav", audioClip.frequency, 1, samplesMono);
            return samplesMono;
        }
        else
        {
            return samplesStereo;
        }
    }
    
    public static float[] GetSamplesOfBeatRangeFromAudioClip(
        SongMeta songMeta,
        AudioClip audioClip,
        int startBeat,
        int lengthInBeats,
        bool convertToMono)
    {
        using DisposableStopwatch ds = new("GetSamplesOfBeatRangeFromAudioClip took <ms>");

        if (lengthInBeats <= 0)
        {
            return null;
        }

        double startBeatInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, startBeat);
        double singleBeatLengthInMillis = BpmUtils.MillisecondsPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = GetAudioSamples(startBeatInMillis, lengthInMillis, audioClip, convertToMono);
        return monoAudioSamples;
    }

    public static float[] GetSamples(float[] samples, int startIndex, int endIndex)
    {
        int lengthInSamples = endIndex - startIndex;
        float[] result = new float[endIndex- startIndex];
        for (int i = 0; i < lengthInSamples; i++)
        {
            result[i] = samples[startIndex + i];
        }

        return result;
    }
}
