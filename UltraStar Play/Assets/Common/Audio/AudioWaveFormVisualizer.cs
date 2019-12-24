using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class AudioWaveFormVisualizer : MonoBehaviour
{
    public Color backgroundColor = new Color(0, 0, 0, 0);
    public Color waveformColor = Color.white;

    private Color[] blank; // blank image array (background color in every pixel)
    private Texture2D texture;

    private RawImage rawImage;
    private RectTransform rectTransform;

    private int textureWidth = 256;
    private int textureHeight = 256;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        CreateTexture();
    }

    public void DrawWaveFormMinAndMaxValues(AudioClip audioClip)
    {
        if (audioClip == null || audioClip.samples == 0)
        {
            return;
        }

        ClearTexture();

        Vector2[] minMaxValues = CalculateMinAndMaxValues(audioClip);
        DrawMinAndMaxValuesToTexture(minMaxValues);
    }

    public void DrawWaveFormValues(float[] samples, int offset, int length)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        ClearTexture();

        // Draw the waveform
        for (int x = 0; x < textureWidth; x++)
        {
            int sampleIndex = offset + length * x / textureWidth;
            if (sampleIndex >= samples.Length)
            {
                break;
            }

            float value = samples[sampleIndex];

            // Draw the pixels
            int y = (int)(textureHeight * (value + 1f) / 2f);
            texture.SetPixel(x, y, waveformColor);
        }

        // upload to the graphics card 
        texture.Apply();
    }

    public void DrawWaveFormMinAndMaxValues(float[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        ClearTexture();

        Vector2[] minMaxValues = CalculateMinAndMaxValues(samples);
        DrawMinAndMaxValuesToTexture(minMaxValues);
    }

    private float[] CopyAudioClipSamples(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples];
        audioClip.GetData(samples, 0);
        return samples;
    }

    private void CreateTexture()
    {
        // The size of the RectTransform can be zero in the first frame, when inside a layout group.
        // See https://forum.unity.com/threads/solved-cant-get-the-rect-width-rect-height-of-an-element-when-using-layouts.377953/
        if (rectTransform.rect.width != 0)
        {
            textureWidth = (int)rectTransform.rect.width;
        }
        if (rectTransform.rect.height != 0)
        {
            textureHeight = (int)rectTransform.rect.height;
        }

        // create the texture and assign to the guiTexture: 
        texture = new Texture2D(textureWidth, textureHeight);
        rawImage.texture = texture;

        // create a 'blank screen' image 
        blank = new Color[textureWidth * textureHeight];
        for (int i = 0; i < blank.Length; i++)
        {
            blank[i] = backgroundColor;
        }

        // reset the texture to the background color
        ClearTexture();
    }

    private void ClearTexture()
    {
        texture.SetPixels(blank, 0);
    }

    private Vector2[] CalculateMinAndMaxValues(float[] samples)
    {
        Vector2[] minMaxValues = new Vector2[textureWidth];

        // calculate window size to fit all samples in the texture
        int windowSize = samples.Length / textureWidth;

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < textureWidth; i++)
        {
            int offset = i * windowSize;
            Vector2 minMax = FindMinAndMaxValues(samples, offset, windowSize);
            minMaxValues[i] = minMax;
        }

        return minMaxValues;
    }

    private Vector2[] CalculateMinAndMaxValues(AudioClip audioClip)
    {
        Vector2[] minMaxValues = new Vector2[textureWidth];

        // calculate window size to fit all samples in the texture
        int windowSize = audioClip.samples / textureWidth;
        float[] windowSamples = new float[windowSize];

        // move the window over all the samples. For each position, find the min and max value.
        for (int i = 0; i < textureWidth; i++)
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
        for (int x = 0; x < textureWidth; x++)
        {
            Vector2 minMax = minMaxValues[x];
            float min = minMax.x;
            float max = minMax.y;

            // Draw the pixels
            int yMin = (int)(textureHeight * (min + 1f) / 2f);
            int yMax = (int)(textureHeight * (max + 1f) / 2f);
            for (int y = yMin; y < yMax; y++)
            {
                texture.SetPixel(x, y, waveformColor);
            }
        }

        // upload to the graphics card 
        texture.Apply();
    }
}
