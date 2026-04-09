using System;
using System.Drawing;
using System.Drawing.Imaging;

/// <summary>
/// Generates visual representations of pitch detection results.
/// </summary>
public class PitchHeatmapGenerator
{
    private const int IMAGE_WIDTH = 1920;
    private const int IMAGE_HEIGHT = 128; // One pixel per MIDI note

    /// <summary>
    /// Generates a MIDI-based heatmap image from an f0 frequency array.
    /// </summary>
    /// <param name="f0Array">The array of fundamental frequencies in Hz for each frame.</param>
    /// <param name="outputPath">The file path where the resulting PNG heatmap will be saved.</param>
    /// <exception cref="ArgumentNullException">Thrown if f0Array is null.</exception>
    public void GenerateHeatmap(float[] f0Array, string outputPath)
    {
        if (f0Array == null) throw new ArgumentNullException(nameof(f0Array));

        int numFrames = f0Array.Length;

        // Create MIDI heatmap [WIDTH, HEIGHT]
        float[,] heatmap = new float[IMAGE_WIDTH, IMAGE_HEIGHT];

        for (int t = 0; t < numFrames; t++)
        {
            float f0 = f0Array[t];
            if (f0 <= 0) continue;

            // Each frame 't' corresponds to a position in the image [0, IMAGE_WIDTH-1]
            int xStart = (int)((double)t / numFrames * IMAGE_WIDTH);
            int xEnd = (int)((double)(t + 1) / numFrames * IMAGE_WIDTH);
            if (xEnd > IMAGE_WIDTH) xEnd = IMAGE_WIDTH;

            int midiNote = (int)Math.Round(12 * Math.Log(f0 / 440.0) / Math.Log(2) + 69);

            if (midiNote >= 0 && midiNote < IMAGE_HEIGHT)
            {
                // Row index is 127 - midiNote to have higher notes at the top
                int y = (IMAGE_HEIGHT - 1) - midiNote;
                for (int x = xStart; x < xEnd; x++)
                {
                    heatmap[x, y] = 1.0f;
                }
            }
        }

        using (Bitmap bitmap = new Bitmap(IMAGE_WIDTH, IMAGE_HEIGHT))
        {
            for (int y = 0; y < IMAGE_HEIGHT; y++)
            {
                for (int x = 0; x < IMAGE_WIDTH; x++)
                {
                    float val = heatmap[x, y];
                    int intensity = (int)(Math.Min(val, 1.0f) * 255);
                    bitmap.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
                }
            }

            bitmap.Save(outputPath, ImageFormat.Png);
        }
    }
}
