using System;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

public class AudioWaveFormVisualizer : MonoBehaviour
{
    public Color waveformColor = Color.white;

    [InjectedInInspector]
    public DynamicallyCreatedImage dynImage;

    public void DrawWaveFormMinAndMaxValues(AudioClip audioClip)
    {
        if (audioClip == null || audioClip.samples == 0)
        {
            return;
        }

        dynImage.ClearTexture();

        Vector2[] minMaxValues = CalculateMinAndMaxValues(audioClip);
        DrawMinAndMaxValuesToTexture(minMaxValues);
    }

    public void DrawWaveFormValues(float[] samples, int offset, int length)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        dynImage.ClearTexture();

        // Draw the waveform
        for (int x = 0; x < dynImage.TextureWidth; x++)
        {
            int sampleIndex = offset + length * x / dynImage.TextureWidth;
            if (sampleIndex >= samples.Length)
            {
                break;
            }

            float value = samples[sampleIndex];

            // Draw the pixels
            int y = (int)(dynImage.TextureHeight * (value + 1f) / 2f);
            dynImage.SetPixel(x, y, waveformColor);
        }

        // upload to the graphics card 
        dynImage.ApplyTexture();
    }

    public void DrawWaveFormMinAndMaxValues(float[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        dynImage.ClearTexture();

        Vector2[] minMaxValues = CalculateMinAndMaxValues(samples);
        DrawMinAndMaxValuesToTexture(minMaxValues);
    }

    private float[] CopyAudioClipSamples(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples];
        audioClip.GetData(samples, 0);
        return samples;
    }

    private Vector2[] CalculateMinAndMaxValues(float[] samples)
    {
        Vector2[] minMaxValues = new Vector2[dynImage.TextureWidth];

        // calculate window size to fit all samples in the texture
        int windowSize = samples.Length / dynImage.TextureWidth;

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynImage.TextureWidth; i++)
        {
            int offset = i * windowSize;
            Vector2 minMax = FindMinAndMaxValues(samples, offset, windowSize);
            minMaxValues[i] = minMax;
        }

        return minMaxValues;
    }

    private Vector2[] CalculateMinAndMaxValues(AudioClip audioClip)
    {
        Vector2[] minMaxValues = new Vector2[dynImage.TextureWidth];

        // calculate window size to fit all samples in the texture
        int windowSize = audioClip.samples / dynImage.TextureWidth;
        float[] windowSamples = new float[windowSize];

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynImage.TextureWidth; i++)
        {
            int offset = i * windowSize;
            audioClip.GetData(windowSamples, offset);
            Vector2 minMax = FindMinAndMaxValues(windowSamples, 0, windowSize);
            minMaxValues[i] = minMax;
        }

        return minMaxValues;
    }

    private Vector2 FindMinAndMaxValues(float[] samples, int offset, int length)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < length; i++)
        {
            int index = offset + i;
            if (index >= samples.Length)
            {
                break;
            }

            float f = samples[index];
            if (f < min)
            {
                min = f;
            }
            if (f > max)
            {
                max = f;
            }
        }
        return new Vector2(min, max);
    }

    private void DrawMinAndMaxValuesToTexture(Vector2[] minMaxValues)
    {
        // Draw the waveform
        for (int x = 0; x < dynImage.TextureWidth; x++)
        {
            Vector2 minMax = minMaxValues[x];
            float min = minMax.x;
            float max = minMax.y;

            // Draw the pixels
            int yMin = (int)(dynImage.TextureHeight * (min + 1f) / 2f);
            int yMax = (int)(dynImage.TextureHeight * (max + 1f) / 2f);
            for (int y = yMin; y < yMax; y++)
            {
                dynImage.SetPixel(x, y, waveformColor);
            }
        }

        // upload to the graphics card 
        dynImage.ApplyTexture();
    }
}
