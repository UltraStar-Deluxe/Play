using System;

public static class AudioMixerUtils
{
    public static float[] Shift(float[] samples, int shift)
    {
        if (samples.IsNullOrEmpty())
        {
            throw new ArgumentException("Sample array must not be null or empty.");
        }

        float[] shiftedSamples = new float[samples.Length];
        if (shift > 0)
        {
            // Right shift: Copy existing samples to the right
            int copyLength = Math.Max(0, samples.Length - shift);
            Array.Copy(samples, 0, shiftedSamples, shift, copyLength);
        }
        else if (shift < 0)
        {
            // Left shift: Copy existing samples to the left
            shift = Math.Abs(shift); // Convert to positive
            int copyLength = Math.Max(0, samples.Length - shift);
            Array.Copy(samples, shift, shiftedSamples, 0, copyLength);
        }
        return shiftedSamples;
    }

    public static float[] Mix(float[] samples1, float[] samples2)
    {
        float[] mixedSamples = new float[Math.Max(samples1.Length, samples2.Length)];
        for (int i = 0; i < Math.Max(samples1.Length, samples2.Length); i++)
        {
            // Simple mixing by averaging
            if (i < samples1.Length && i < samples2.Length)
            {
                mixedSamples[i] = (samples1[i] + samples2[i]) / 2;
            }
            else if (i < samples1.Length)
            {
                mixedSamples[i] = samples1[i];
            }
            else if (i < samples2.Length)
            {
                mixedSamples[i] = samples2[i];
            }
            else
            {
                mixedSamples[i] = 0;
            }
        }

        return mixedSamples;
    }

    public static void Normalize(float[] samples, float targetVolume)
    {
        if (samples.IsNullOrEmpty())
        {
            throw new ArgumentException("Sample array must not be empty.");
        }

        if (targetVolume < 0
            || targetVolume > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetVolume), "Target volume must be between 0.0 and 1.0.");
        }

        // Find the maximum absolute value in the sample array
        float maxAmplitude = 0.0f;
        foreach (float sample in samples)
        {
            maxAmplitude = Math.Max(maxAmplitude, Math.Abs(sample));
        }

        // If the maximum sample is 0, the array is silent, so no normalization is needed
        if (maxAmplitude == 0.0f
            || maxAmplitude >= targetVolume)
        {
            return;
        }

        // Scale all samples so that the maximum value becomes the target volume
        float scaleFactor = targetVolume / maxAmplitude;
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= scaleFactor;
        }
    }

    public static float[] Resample(float[] originalSamples, int sourceSampleRate, int targetSampleRate)
    {
        float resampleRatio = (float)targetSampleRate / sourceSampleRate;
        int totalResampledSamples = (int)Math.Ceiling(originalSamples.Length * resampleRatio);
        float[] resampledSamples = new float[totalResampledSamples];

        // Perform linear interpolation to fill the resampled array
        for (int i = 0; i < totalResampledSamples; i++)
        {
            // Map the resampled index to the original index
            float originalIndex = i / resampleRatio;

            // Get the indices of the surrounding samples
            int index1 = (int)Math.Floor(originalIndex);
            int index2 = Math.Min(index1 + 1, originalSamples.Length - 1);

            // Calculate the interpolation factor
            float t = originalIndex - index1;

            // Perform linear interpolation
            resampledSamples[i] = (1 - t) * originalSamples[index1] + t * originalSamples[index2];
        }

        return resampledSamples;
    }
}