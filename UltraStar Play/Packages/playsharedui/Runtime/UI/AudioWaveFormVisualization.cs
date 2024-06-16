using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioWaveFormVisualization : INeedInjection, IDisposable
{
    public Color WaveformColor { get; set; }= Color.white;

    private readonly DynamicTexture dynTexture;

    private FixLengthList<MinMax> minMaxValues;
    
    public AudioWaveFormVisualization(
        GameObject gameObject,
        VisualElement visualElement,
        int textureWidth = -1,
        int textureHeight = -1,
        string name = null)
    {
        dynTexture = new DynamicTexture(gameObject, visualElement, textureWidth, textureHeight, name);
    }

    public void Dispose()
    {
        dynTexture.Dispose();
    }

    public void DrawWaveFormMinAndMaxValues(AudioClip audioClip, int minSampleSingleChannel = -1, int maxSampleSingleChannel = -1)
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

        CalculateMinAndMaxValues(audioClip, minSampleSingleChannel, maxSampleSingleChannel);
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

    public void DrawWaveFormMinAndMaxValues(float[] samples, int minSample = -1, int maxSample = -1)
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

        CalculateMinAndMaxValues(samples, minSample, maxSample);
        DrawMinAndMaxValuesToTexture();
    }

    private void CalculateMinAndMaxValues(float[] samples, int minSample, int maxSample)
    {
        PrepareMinMaxValues(dynTexture.TextureWidth);

        // calculate window size to fit all samples in the texture
        int lengthInSamples = maxSample - minSample;
        if (lengthInSamples <= dynTexture.TextureWidth)
        {
            Debug.LogWarning("Too few samples to draw audio waveform");
            return;
        }
        
        int windowSize = lengthInSamples / dynTexture.TextureWidth;

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynTexture.TextureWidth; i++)
        {
            int offset = minSample + i * windowSize;
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

    private void CalculateMinAndMaxValues(AudioClip audioClip, int minSampleSingleChannel, int maxSampleSingleChannel)
    {
        PrepareMinMaxValues(dynTexture.TextureWidth);

        // calculate window size to fit all samples in the texture
        int lengthInSamples = maxSampleSingleChannel - minSampleSingleChannel;
        int windowSizeInSamples = lengthInSamples / dynTexture.TextureWidth;
        float[] windowSamples = new float[windowSizeInSamples];

        Log.Debug(() => $"AudioWaveForm windowSizeInSamples {((double)lengthInSamples / dynTexture.TextureWidth)}");
        Log.Debug(() => $"AudioWaveForm minSample {minSampleSingleChannel} maxSample {maxSampleSingleChannel} lengthInSamples: {lengthInSamples} windowSizeInSamples {windowSizeInSamples}");

        // Move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < dynTexture.TextureWidth; i++)
        {
            int offset = i * windowSizeInSamples;
            if (windowSizeInSamples + offset >= lengthInSamples)
            {
                // Not enough samples left
                minMaxValues[i].min = 0;
                minMaxValues[i].max = 0;
                continue;
            }
            audioClip.GetData(windowSamples, minSampleSingleChannel + offset);
            FindMinAndMaxValues(windowSamples, 0, windowSizeInSamples, out float min, out float max);
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
                min = 0;
                max = 0;
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
