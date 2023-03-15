using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioWaveFormVisualization : INeedInjection
{
    public Color WaveformColor { get; set; }= Color.white;

    private readonly DynamicTexture dynTexture;

    private FixLengthList<MinMax> minMaxValues;
    
    public AudioWaveFormVisualization(GameObject gameObject, VisualElement visualElement)
    {
        dynTexture = new DynamicTexture(gameObject, visualElement);
    }

    public void Destroy()
    {
        dynTexture.Destroy();
    }

    public void DrawWaveFormMinAndMaxValues(AudioClip audioClip)
    {
        if (audioClip == null || audioClip.samples == 0)
        {
            return;
        }

        dynTexture.ClearTexture();

        CalculateMinAndMaxValues(audioClip);
        DrawMinAndMaxValuesToTexture();
    }

    public void DrawWaveFormValues(float[] samples, int offset, int length)
    {
        if (samples == null || samples.Length == 0
            || !dynTexture.IsInitialized)
        {
            return;
        }

        dynTexture.ClearTexture();

        // Draw the waveform
        for (int x = 0; x < dynTexture.TextureWidth; x++)
        {
            int sampleIndex = offset + length * x / dynTexture.TextureWidth;
            if (sampleIndex >= samples.Length)
            {
                break;
            }

            float value = samples[sampleIndex];

            // Draw the pixels
            int y = (int)(dynTexture.TextureHeight * (value + 1f) / 2f);
            dynTexture.SetPixel(x, y, WaveformColor);
        }

        // upload to the graphics card 
        dynTexture.ApplyTexture();
    }

    public void DrawWaveFormMinAndMaxValues(float[] samples)
    {
        if (samples.IsNullOrEmpty()
            || !dynTexture.IsInitialized)
        {
            return;
        }

        dynTexture.ClearTexture();

        CalculateMinAndMaxValues(samples);
        DrawMinAndMaxValuesToTexture();
    }

    private void CalculateMinAndMaxValues(float[] samples)
    {
        PrepareMinMaxValues(dynTexture.TextureWidth);

        // calculate window size to fit all samples in the texture
        int windowSize = samples.Length / dynTexture.TextureWidth;

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynTexture.TextureWidth; i++)
        {
            int offset = i * windowSize;
            FindMinAndMaxValues(samples, offset, windowSize, out float min, out float max);
            minMaxValues[i].min = min;
            minMaxValues[i].max = max;
        }
    }

    private void PrepareMinMaxValues(int size)
    {
        if (minMaxValues == null)
        {
            minMaxValues = new FixLengthList<MinMax>(size, () => new MinMax());
            return;
        }
        
        minMaxValues.FixLength = size;
    }

    private void CalculateMinAndMaxValues(AudioClip audioClip)
    {
        PrepareMinMaxValues(dynTexture.TextureWidth);

        // calculate window size to fit all samples in the texture
        int windowSize = audioClip.samples / dynTexture.TextureWidth;
        float[] windowSamples = new float[windowSize];

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynTexture.TextureWidth; i++)
        {
            int offset = i * windowSize;
            audioClip.GetData(windowSamples, offset);
            FindMinAndMaxValues(windowSamples, 0, windowSize, out float min, out float max);
            minMaxValues[i].min = min;
            minMaxValues[i].max = max;
        }
    }

    private void FindMinAndMaxValues(float[] samples, int offset, int length, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;

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
    }

    private void DrawMinAndMaxValuesToTexture()
    {
        // Draw the waveform
        for (int x = 0; x < dynTexture.TextureWidth; x++)
        {
            MinMax minMax = minMaxValues[x];
            float min = minMax.min;
            float max = minMax.max;

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

    private class MinMax
    {
        public float min;
        public float max;

        public MinMax()
        {
        }

        public MinMax(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public void Reset()
        {
            min = 0;
            max = 0;
        }
    }
}
