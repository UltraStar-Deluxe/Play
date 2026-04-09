using System;
using System.IO;
using UnityEngine;

public class RmvpePitchDetectionDemo : MonoBehaviour
{
    async void Start()
    {
        _ = DetectPitchAsync();
    }
    
    private async Awaitable DetectPitchAsync()
    {
        string filePath =
            "C:/Dev/Projects/GitHub/achimmihca/VlcForUnityPlayground/Assets/StreamingAssets/Stone Sour - Through Glass - excerpt2.ogg";
        if (!File.Exists(filePath))
        {
            throw new Exception($"File does not exist: {filePath}");
        }

        AudioClip audioClip = await AudioManager.LoadAudioClipFromUriAsync($"file://{filePath}", false);
        Debug.Log("Loaded audioClip: " + audioClip);
        float[] monoAudioSamples = AudioSampleUtils.GetAudioSamples(audioClip, 0, audioClip.length * 1000, true);
        float[] monoAudioSamplesResampled = AudioSampleUtils.Resample(monoAudioSamples, audioClip.frequency, RmvpePitchDetector.ExpectedSampleRate);
        Debug.Log("Resampled mono audio samples: " + monoAudioSamplesResampled.Length);

        string modelPath = ApplicationUtils.GetStreamingAssetsPath("AiModels/rmvpe/rmvpe_20231006.onnx");
        using RmvpePitchDetector pitchDetector = new RmvpePitchDetector(modelPath);
        RmvpePitchResult result = pitchDetector.DetectPitch(monoAudioSamplesResampled);

        PitchHeatmapGenerator pitchHeatmapGenerator = new PitchHeatmapGenerator();
        pitchHeatmapGenerator.GenerateHeatmap(result.Frequencies, ApplicationUtils.GetPersistentDataPath("RmvpePitchResult.png"));
        PitchAudioGenerator pitchAudioGenerator = new PitchAudioGenerator();
        pitchAudioGenerator.GenerateWav(result.Estimates, ApplicationUtils.GetPersistentDataPath("RmvpePitchResult.wav"), audioClip.length, RmvpePitchDetector.ExpectedSampleRate);
    }
}
