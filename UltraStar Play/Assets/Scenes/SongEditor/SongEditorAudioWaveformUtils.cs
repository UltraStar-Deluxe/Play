using System;
using System.IO;
using UnityEngine;

public static class SongEditorAudioWaveformUtils
{
    public static bool IsSupportedAudioFormat(SongMeta songMeta, Settings settings)
    {
        return IsSupportedAudioFormat(GetAudioUri(songMeta, GetAudioWaveformSamplesSource(settings)));
    }

    public static bool IsSupportedAudioFormat(string audioUri)
    {
        if (ApplicationUtils.IsSupportedMidiFormat(Path.GetExtension(new Uri(audioUri).LocalPath)))
        {
            return false;
        }

        // Must be an audio format supported by Unity to get all the samples
        return ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(audioUri));
        
        // As alternative, if ffmpeg libraries are available, can load any format as samples.
        // return true;
    }
    
    public static async Awaitable<AudioClip> GetAudioClipToDrawAudioWaveform(
        SongMeta songMeta,
        Settings settings,
        AudioSampleLoader audioSampleLoader)
    {
        // using IDisposable d = new DisposableStopwatch($"Get audio clip to draw audio wave form");

        ESongEditorSamplesSource samplesSource = GetAudioWaveformSamplesSource(settings);
        string audioUri = GetAudioUri(songMeta, samplesSource);
        if (audioUri.IsNullOrEmpty())
        {
            Debug.LogWarning($"No {samplesSource} audio found. Split the audio first. Using original music instead.");
            audioUri = GetAudioUri(songMeta, ESongEditorSamplesSource.OriginalMusic);
        }

        if (!SongMetaUtils.ResourceExists(songMeta, audioUri))
        {
            Debug.Log($"Audio file resource does not exist {audioUri}");
            return null;
        }

        if (!IsSupportedAudioFormat(audioUri))
        {
            return null;
        }

        AudioClip audioClip = await audioSampleLoader.LoadAsAudioClip(audioUri);
        return audioClip;
    }

    private static ESongEditorSamplesSource GetAudioWaveformSamplesSource(Settings settings)
    {
        switch (settings.SongEditorSettings.AudioWaveformSamplesSource)
        {
            case ESongEditorAudioWaveformSamplesSource.Vocals:
                return ESongEditorSamplesSource.Vocals;
            case ESongEditorAudioWaveformSamplesSource.Instrumental:
                return ESongEditorSamplesSource.Instrumental;
            case ESongEditorAudioWaveformSamplesSource.OriginalMusic:
                return ESongEditorSamplesSource.OriginalMusic;
            case ESongEditorAudioWaveformSamplesSource.SameAsPlayback:
            {
                if (settings.SongEditorSettings.PlaybackSamplesSource is ESongEditorSamplesSource.Recording)
                {
                    return ESongEditorSamplesSource.OriginalMusic;
                }
                else
                {
                    return settings.SongEditorSettings.PlaybackSamplesSource;
                }
            }
            default:
                return ESongEditorSamplesSource.OriginalMusic;
        }
    }

    public static void DrawAudioWaveform(
        AudioWaveFormVisualization audioWaveFormVisualization,
        AudioClip audioClip,
        int minSampleSingleChannel = -1,
        int maxSampleSingleChannel = -1)
    {
        if (audioClip == null
            || audioWaveFormVisualization == null)
        {
            return;
        }

        // using IDisposable d = new DisposableStopwatch($"Draw audio wave form");
        audioWaveFormVisualization.DrawAudioWaveForm(audioClip, minSampleSingleChannel, maxSampleSingleChannel);
    }

    public static void DrawAudioWaveform(
        AudioWaveFormVisualization audioWaveFormVisualization,
        float[] samples,
        int minSample = -1,
        int maxSample = -1)
    {
        if (samples.IsNullOrEmpty()
            || audioWaveFormVisualization == null)
        {
            return;
        }

        // using IDisposable d = new DisposableStopwatch($"Draw audio wave form");
        audioWaveFormVisualization.DrawAudioWaveForm(samples, minSample, maxSample);
    }
    
    private static string GetAudioUri(SongMeta songMeta, ESongEditorSamplesSource samplesSource)
    {
        switch (samplesSource)
        {
            case ESongEditorSamplesSource.Instrumental:
                return SongMetaUtils.GetInstrumentalAudioUri(songMeta);
            case ESongEditorSamplesSource.Vocals:
                return SongMetaUtils.GetVocalsAudioUri(songMeta);
            default:
                return SongMetaUtils.GetAudioUri(songMeta);
        }
    }
}
