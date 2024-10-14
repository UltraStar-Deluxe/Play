using System;
using UnityEngine;
using UnityEngine.Networking;

public static class AudioUtils
{
    public static void SetPitchWithPitchShifter(AudioSource audioSource, float pitch)
    {
        if (audioSource == null
            || Math.Abs(audioSource.pitch - pitch) < 0.01f)
        {
            return;
        }

        if (audioSource.outputAudioMixerGroup == null
            || audioSource.outputAudioMixerGroup.audioMixer == null)
        {
            audioSource.outputAudioMixerGroup = AudioManager.Instance.pitchShifterAudioMixerGroup;
        }

        // Setting the pitch of an AudioPlayer will change tempo and pitch.
        audioSource.pitch = pitch;

        // A Pitch Shifter effect on an AudioMixerGroup can be used to compensate the pitch change of the AudioPlayer,
        // such that only the change of the tempo remains.
        // See here for details: https://answers.unity.com/questions/25139/how-i-can-change-the-speed-of-a-song-or-sound.html
        // See here for how the pitch value of the Pitch Shifter effect is made available for scripting: https://learn.unity.com/tutorial/audio-mixing#5c7f8528edbc2a002053b506
        audioSource.outputAudioMixerGroup.audioMixer.SetFloat("PitchShifter.Pitch", 1 + (1 - pitch));
    }

    public static void ResetPitchAndPitchShifter(AudioSource audioSource)
    {
        SetPitchWithPitchShifter(audioSource, 1);
    }

    public static UnityWebRequest CreateAudioClipRequest(Uri uriHandle, bool streamAudio)
    {
        UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uriHandle, AudioType.UNKNOWN);
        DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
        downloadHandler.streamAudio = streamAudio;
        return webRequest;
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

        if (startIndex < 0
            || endIndex < 0)
        {
            Debug.LogError($"ToShortSampleArray called with invalid index. startIndex: {startIndex}, endIndex: {endIndex}");
            return shortSampleArray;
        }

        for (int i = 0; i < lengthInSamples && i + startIndex < floatSampleArray.Length; i++)
        {
            shortSampleArray[i] = (short)Math.Floor(floatSampleArray[i + startIndex] * short.MaxValue);
        }

        return shortSampleArray;
    }

    public static float[] GetAudioSamples(AudioClip audioClip, double startInMillis, double lengthInMillis, bool convertToMono)
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

    public static float[] GetAudioSamples(AudioClip audioClip, int channel)
    {
        float[] singleChannelSamples = new float[audioClip.samples];
        float[] allSamples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(allSamples, 0);

        // Fill the single channel array with the samples of the selected channel
        for (int i = 0; i < singleChannelSamples.Length; i++)
        {
            singleChannelSamples[i] = allSamples[i * audioClip.channels + channel];
        }

        return singleChannelSamples;
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

        double startBeatInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, startBeat);
        double singleBeatLengthInMillis = SongMetaBpmUtils.MillisPerBeat(songMeta);
        double lengthInMillis = singleBeatLengthInMillis * lengthInBeats;

        float[] monoAudioSamples = GetAudioSamples(audioClip, startBeatInMillis, lengthInMillis, convertToMono);
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

    public static float[] Resample(float[] source, int oldSampleRate, int newSampleRate)
    {
        if (oldSampleRate == newSampleRate)
        {
            return source;
        }

        using DisposableStopwatch d = new($"Resample of {source.Length} samples to {newSampleRate} from {oldSampleRate} took <ms>");

        int sourceArrayLength = source.Length;
        int newArrayLength = (int)(sourceArrayLength * ((double)newSampleRate / (double)oldSampleRate));
        return ResampleByLength(source, newArrayLength);
    }

    private static float[] ResampleByLength(float[] source, int n)
    {
        // https://stackoverflow.com/questions/28874894/float-array-resampling
        // n destination length
        int m = source.Length; // source length
        float[] destination = new float[n];
        destination[0] = source[0];
        destination[n-1] = source[m-1];

        for (int i = 1; i < n-1; i++)
        {
            float jd = ((float)i * (float)(m - 1) / (float)(n - 1));
            int j = (int)jd;
            destination[i] = source[j] + (source[j + 1] - source[j]) * (jd - (float)j);
        }
        return destination;
    }
}
