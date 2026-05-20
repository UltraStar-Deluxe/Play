using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class OggFileWriterTest
{
    [Test]
    public async Task WriteAndLoadOggFileTest(
        [Values(1, 2)] int channels,
        [Values(16000, 44100, 48000, 96000)] int sampleRate,
        [Values(0.1f, 0.4f, 0.9f, 1f)] float quality)
    {
        Debug.Log($"Running test case. channels: {channels}, sampleRate: {sampleRate}, quality: {quality.ToStringInvariantCulture()}");
        
        int durationInSeconds = 2;
        float[] samples = GenerateAudioSamples(channels, sampleRate, durationInSeconds);

        string outputPath = $"{Application.temporaryCachePath}/OggFileWriterTest/test_{channels}_{sampleRate}_{quality.ToStringInvariantCulture()}.ogg";
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        Debug.Log($"Writing file to temporary location. path: '{outputPath}'");
        OggFileWriter.WriteFile(outputPath, sampleRate, channels, samples, quality);

        Assert.IsTrue(File.Exists(outputPath), "Ogg file was not created.");

        // Load the file with Unity API
        AudioClip loadedAudioClip = await AudioClipTestUtils.LoadAudioClipAsync(outputPath);

        Assert.IsNotNull(loadedAudioClip, "Loaded AudioClip is null.");
        Assert.AreEqual(channels, loadedAudioClip.channels, "Channel count mismatch.");
        Assert.AreEqual(sampleRate, loadedAudioClip.frequency, "Sample rate mismatch.");
        // Ogg encoding might slightly shift duration, so check with some tolerance
        Assert.AreEqual(durationInSeconds, loadedAudioClip.length, 0.1, "Duration mismatch.");

        Object.DestroyImmediate(loadedAudioClip);
    }

    private static float[] GenerateAudioSamples(int channels, int sampleRate, int durationInSeconds)
    {
        float pitchInHz = 440; // A4
        int totalSamplesPerChannel = sampleRate * durationInSeconds;
        float[] samples = new float[totalSamplesPerChannel * channels];

        for (int i = 0; i < totalSamplesPerChannel; i++)
        {
            float value = Mathf.Sin(2 * Mathf.PI * pitchInHz * i / sampleRate);
            for (int ch = 0; ch < channels; ch++)
            {
                samples[i * channels + ch] = value;
            }
        }

        return samples;
    }
}
