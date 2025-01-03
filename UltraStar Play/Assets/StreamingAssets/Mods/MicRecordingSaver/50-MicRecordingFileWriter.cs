using System;
using System.IO;
using UniInject;
using UnityEngine;

public class MicRecordingFileWriter : INeedInjection
{
    [Inject]
    private ModObjectContext modObjectContext;

    [Inject]
    private SongMeta songMeta;

    public void SaveAll()
    {
        MicRecordingData.PlayerProfileToMicRecording.ForEach(entry =>
        {
            PlayerProfile playerProfile = entry.Key;
            MicRecordingData micRecordingData = entry.Value;
            Save(playerProfile, micRecordingData);
        });

        NotificationManager.CreateNotification(Translation.Of($"Mic input saved to {GetTargetDirectory()}"));
        
        ApplicationUtils.OpenDirectory(GetTargetDirectory());
    }

    private void Save(PlayerProfile playerProfile, MicRecordingData micRecordingData)
    {
        int micSampleRate = micRecordingData.MicSampleRate;
        if (micSampleRate <= 0)
        {
            return;
        }

        // Try to get instrumental audio to save a mix with the mic recording (must happen on main thread because of Unity API)
        bool hasInstrumentalSamples = TryLoadInstrumentalSamples(songMeta, out float[] instrumentalSamples, out int instrumentalSampleRate);

        // Create array with only the written samples
        float[] writtenMicSamples = new float[micRecordingData.WrittenMicSampleCount];
        Array.Copy(micRecordingData.MicSamples, writtenMicSamples, micRecordingData.WrittenMicSampleCount);

        // Shift samples by overall recording delay
        int overallDelayInSamples = (int)(micRecordingData.OverallDelayInMillis * 0.001 * micSampleRate);
        Debug.Log("overallDelayInSamples: " + overallDelayInSamples);
        AudioMixerUtils.Shift(writtenMicSamples, -overallDelayInSamples);

        // Normalize samples
        AudioMixerUtils.Normalize(writtenMicSamples, 0.75f);

        // Save recorded samples
        int monoChannelCount = 1;
        string targetFilePath = GetTargetFilePath(playerProfile.Name);
        WavFileWriter.WriteFile(targetFilePath, micSampleRate, monoChannelCount, writtenMicSamples);
        Debug.Log($"Mic recording saved to '{targetFilePath}'");

        // Save mix with instrumental audio
        if(hasInstrumentalSamples)
        {
            // Save instrumental samples
            // string instrumentalTargetFilePath = GetTargetFilePath("instrumental");
            // WavFileWriter.WriteFile(instrumentalTargetFilePath, instrumentalSampleRate, monoChannelCount, instrumentalSamples);
            // Debug.Log($"Instrumental samples saved to '{instrumentalTargetFilePath}'");

            // Save mixed samples
            float[] resampledMicSamples = AudioMixerUtils.Resample(writtenMicSamples, micSampleRate, instrumentalSampleRate);
            float[] mixedSamples = AudioMixerUtils.Mix(instrumentalSamples, resampledMicSamples);
            string mixTargetFilePath = GetTargetFilePath($"{playerProfile.Name} + instrumental");
            WavFileWriter.WriteFile(mixTargetFilePath, instrumentalSampleRate, monoChannelCount, mixedSamples);
            Debug.Log($"Mix of instrumental and mic recording saved to '{mixTargetFilePath}'");
        }
    }

    private bool TryLoadInstrumentalSamples(SongMeta songMeta, out float[] instrumentalSamples, out int instrumentalSampleRate)
    {
        string instrumentalAudioFilePath = SongMetaUtils.GetInstrumentalAudioUri(songMeta);
        if(!File.Exists(instrumentalAudioFilePath))
        {
            instrumentalSamples = null;
            instrumentalSampleRate = 0;
            return false;
        }

        try
        {
            AudioClip audioClip = AudioManager.LoadAudioClipFromUriImmediately(instrumentalAudioFilePath, false);
            instrumentalSamples = GetMonoSamples(audioClip);
            instrumentalSampleRate = audioClip.frequency;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load instrumental audio from '{instrumentalAudioFilePath}': {ex.Message}");
            instrumentalSamples = null;
            instrumentalSampleRate = 0;
            return false;
        }
    }

    private float[] GetMonoSamples(AudioClip audioClip)
    {
        // Get the total number of samples and channels
        int totalSamples = audioClip.samples;
        int channels = audioClip.channels;

        // Retrieve the audio data
        float[] multiChannelSamples = new float[totalSamples * channels];
        audioClip.GetData(multiChannelSamples, 0);

        // Convert to mono
        float[] monoSamples = AudioUtils.ToMonoAudioSamples(multiChannelSamples, channels);
        return monoSamples;
    }
    
    private string GetTargetDirectory()
    {
        return $"{modObjectContext.ModPersistentDataFolder}/Recordings/{songMeta.GetArtistDashTitle()}";
    }

    private string GetTargetFilePath(string fileBaseName)
    {
        return $"{GetTargetDirectory()}/{fileBaseName}.wav";
    }
}