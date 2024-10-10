using System;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioWaveFormVisualization : INeedInjection, IDisposable
{
    public Color WaveformColor { get; set; }= Color.white;

    private readonly DynamicTexture dynTexture;

    public IAudioWaveFormCalculator AudioWaveFormCalculator { get; set; }

    public AudioWaveFormVisualization(
        GameObject gameObject,
        VisualElement visualElement,
        int textureWidth = -1,
        int textureHeight = -1,
        string name = null)
    {
        AudioWaveFormCalculator = new AudioWaveFormCalculator();
        dynTexture = new DynamicTexture(gameObject, visualElement, textureWidth, textureHeight, name);
    }

    public void Dispose()
    {
        dynTexture.Dispose();
    }

    public void DrawAudioWaveForm(AudioClip audioClip, int minSampleSingleChannel = -1, int maxSampleSingleChannel = -1)
    {
        if (audioClip == null || audioClip.samples == 0)
        {
            return;
        }

        if (minSampleSingleChannel < 0)
        {
            minSampleSingleChannel = 0;
        }
        if (maxSampleSingleChannel < 0)
        {
            maxSampleSingleChannel = audioClip.samples;
        }

        if (maxSampleSingleChannel < minSampleSingleChannel)
        {
            Debug.LogError("maxSample must be greater than minSample");
            return;
        }

        int lengthInSamples = maxSampleSingleChannel - minSampleSingleChannel;
        if (lengthInSamples <= dynTexture.TextureWidth)
        {
            Debug.LogWarning("Too few samples to draw audio waveform");
            return;
        }

        dynTexture.ClearTexture();

        AudioWaveForm audioWaveForm = CalculateAudioWaveForm(audioClip, minSampleSingleChannel, maxSampleSingleChannel, dynTexture.TextureWidth);
        DrawAmplitudeRanges(audioWaveForm.AmplitudeRanges);
    }

    public void DrawAudioWaveForm(float[] samples, int minSample = -1, int maxSample = -1)
    {
        if (samples.IsNullOrEmpty()
            || !dynTexture.IsInitialized)
        {
            return;
        }

        if (minSample < 0)
        {
            minSample = 0;
        }
        if (maxSample < 0)
        {
            maxSample = samples.Length - 1;
        }

        if (maxSample < minSample)
        {
            Debug.LogError("maxSample must be greater than minSample");
            return;
        }

        int lengthInSamples = maxSample - minSample;
        if (lengthInSamples <= dynTexture.TextureWidth)
        {
            Debug.LogWarning("Too few samples to draw audio waveform");
            return;
        }

        dynTexture.ClearTexture();

        int windowSize = (maxSample - minSample) / dynTexture.TextureWidth;
        AudioWaveForm audioWaveForm = AudioWaveFormCalculator.Calculate(samples, windowSize, minSample, maxSample);
        DrawAmplitudeRanges(audioWaveForm.AmplitudeRanges);
    }

    private AudioWaveForm CalculateAudioWaveForm(AudioClip audioClip, int minSampleSingleChannel, int maxSampleSingleChannel, int count)
    {
        using IDisposable d = new DisposableStopwatch("CalculateMinAndMaxAmplitude - AudioClip");

        // calculate window size to fit all samples in the texture
        int lengthInSamples = maxSampleSingleChannel - minSampleSingleChannel;
        int windowSizeInSamples = lengthInSamples / count;
        float[] windowSamples = new float[windowSizeInSamples];

        Log.Debug(() => $"AudioWaveForm windowSizeInSamples {((double)lengthInSamples / count)}");
        Log.Debug(() => $"AudioWaveForm minSample {minSampleSingleChannel} maxSample {maxSampleSingleChannel} lengthInSamples: {lengthInSamples} windowSizeInSamples {windowSizeInSamples}");

        List<AmplitudeRange> amplitudeRanges = new List<AmplitudeRange>(count);

        // Move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynTexture.TextureWidth; i++)
        {
            int offset = i * windowSizeInSamples;
            if (windowSizeInSamples + offset >= lengthInSamples)
            {
                // Not enough samples left
                amplitudeRanges.Add(new AmplitudeRange());
                continue;
            }
            audioClip.GetData(windowSamples, minSampleSingleChannel + offset);
            AudioWaveFormUtils.FindMinAndMaxValues(windowSamples, 0, windowSizeInSamples, out float min, out float max);
            amplitudeRanges.Add(new AmplitudeRange(min, max));
        }

        return new AudioWaveForm(amplitudeRanges);
    }

    private void DrawAmplitudeRanges(List<AmplitudeRange> amplitudeRanges)
    {
        for (int x = 0; x < dynTexture.TextureWidth && x < amplitudeRanges.Count; x++)
        {
            AmplitudeRange amplitudeRange = amplitudeRanges[x];
            float min = amplitudeRange.Min;
            float max = amplitudeRange.Max;

            // Draw the pixels
            int yMin = (int)(dynTexture.TextureHeight * (min + 1f) / 2f);
            int yMax = (int)(dynTexture.TextureHeight * (max + 1f) / 2f);
            for (int y = yMin; y < yMax; y++)
            {
                dynTexture.SetPixel(x, y, WaveformColor);
            }
        }

        // upload to the graphics card
        dynTexture.ApplyTexture();
    }
}
