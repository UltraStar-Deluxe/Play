using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class AudioWaveFormVisualizer : MonoBehaviour
{
    public Color backgroundColor = Color.black;
    public Color waveformColor = Color.green;

    private Color[] blank; // blank image array (background color in every pixel)
    private Texture2D texture;

    private RawImage rawImage;
    private RectTransform rectTransform;

    private int textureWidth;
    private int textureHeight;

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

        float[] samples = CopyAudioClipSamples(audioClip);
        DrawWaveFormMinAndMaxValues(samples);
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
        textureWidth = (int)rectTransform.rect.width;
        textureHeight = (int)rectTransform.rect.height;

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
