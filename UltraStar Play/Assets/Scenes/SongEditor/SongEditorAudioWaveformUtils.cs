using System;
using System.IO;
using UnityEngine;

public static class SongEditorAudioWaveformUtils
{
    public static AudioClip GetAudioClipToDrawAudioWaveform(
        SongMeta songMeta,
        AudioManager audioManager,
        Settings settings)
    {
        using IDisposable d = new DisposableStopwatch($"Get audio clip to draw audio wave form");
        
        string audioUri = GetAudioUri(songMeta, settings.SongEditorSettings.PlaybackSamplesSource);
        if (audioUri.IsNullOrEmpty())
        {
            Debug.LogWarning($"No {settings.SongEditorSettings.PlaybackSamplesSource} audio found. Split the audio first. Using original music instead.");
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

        // For drawing the waveform, the AudioClip must not be streamed. All data must have been fully loaded.
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(audioUri, false);
        return audioClip;
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
        
        using IDisposable d = new DisposableStopwatch($"Draw audio wave form");
        audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(audioClip, minSampleSingleChannel, maxSampleSingleChannel);
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
        
        using IDisposable d = new DisposableStopwatch($"Draw audio wave form");
        audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(samples, minSample, maxSample);
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
