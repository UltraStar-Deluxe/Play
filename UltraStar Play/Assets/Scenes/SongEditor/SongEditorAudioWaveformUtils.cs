using System;
using System.IO;
using UnityEngine;

public static class SongEditorAudioWaveformUtils
{
    public static async Awaitable<AudioClip> GetAudioClipToDrawAudioWaveform(
        SongMeta songMeta,
        Settings settings)
    {
        // using IDisposable d = new DisposableStopwatch($"Get audio clip to draw audio wave form");

        ESongEditorSamplesSource samplesSource = GetAudioWaveformSamplesSource(settings);
        string audioUri = GetAudioUri(songMeta, samplesSource);
        if (audioUri.IsNullOrEmpty())
        {
            Debug.LogWarning($"No {samplesSource} audio found. Split the audio first. Using original music instead.");
            audioUri = GetAudioUri(songMeta, ESongEditorSamplesSource.OriginalMusic);
        }

        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            Debug.Log($"Audio file resource does not exist {audioUri}");
            return null;
        }

        string fileExtension = Path.GetExtension(new Uri(audioUri).LocalPath);
        if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            // Cannot draw audio wave form of MIDI file.
            return null;
        }

        if (!ApplicationUtils.IsUnitySupportedAudioFormat(fileExtension))
        {
            // Cannot load this format using Unity API.
            return null;
        }

        // For drawing the waveform, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync(audioUri, false);
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
