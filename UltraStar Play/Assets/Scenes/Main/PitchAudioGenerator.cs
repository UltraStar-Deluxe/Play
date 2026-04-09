using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class PitchAudioGenerator
{
    public void GenerateWav(List<RmvpePitchEstimate> estimates, string outputPath, double totalDurationInSeconds,
        int sampleRate = 44100)
    {
        if (estimates == null) throw new ArgumentNullException(nameof(estimates));

        int totalSamples = (int)(totalDurationInSeconds * sampleRate);
        float[] outputBuffer = new float[totalSamples];

        double phase = 0;
        double maxAmplitude = 0.5;

        // Sort estimates by time
        var sortedEstimates = estimates.OrderBy(e => e.Time).ToList();
        int estimateIndex = 0;
        double frameDuration = 0.01; // 10ms

        for (int i = 0; i < totalSamples; i++)
        {
            double currentTime = (double)i / sampleRate;

            // Move to the estimate that includes the current time
            while (estimateIndex < sortedEstimates.Count - 1 &&
                   currentTime >= sortedEstimates[estimateIndex].Time + frameDuration)
            {
                estimateIndex++;
            }

            double frequency = 0;
            if (estimateIndex < sortedEstimates.Count)
            {
                var estimate = sortedEstimates[estimateIndex];
                if (currentTime >= estimate.Time && currentTime < estimate.Time + frameDuration)
                {
                    frequency = estimate.Frequency;
                }
            }

            if (frequency > 0)
            {
                outputBuffer[i] = (float)(maxAmplitude * Math.Sin(phase));
                phase += 2 * Math.PI * frequency / sampleRate;
                if (phase > 2 * Math.PI)
                {
                    phase -= 2 * Math.PI;
                }
            }
            else
            {
                outputBuffer[i] = 0;
            }
        }

        SaveWav(outputPath, outputBuffer, sampleRate);
    }

    private void SaveWav(string filePath, float[] buffer, int sampleRate)
    {
        using (var fs = new FileStream(filePath, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            // WAV Header
            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + buffer.Length * 2); // File size - 8
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(16); // Subchunk1Size (16 for PCM)
            bw.Write((short)1); // AudioFormat (1 for PCM)
            bw.Write((short)1); // NumChannels (1 for mono)
            bw.Write(sampleRate); // SampleRate
            bw.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
            bw.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
            bw.Write((short)16); // BitsPerSample

            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(buffer.Length * 2); // Subchunk2Size (NumSamples * NumChannels * BitsPerSample/8)

            // WAV Data (16-bit PCM)
            foreach (var sample in buffer)
            {
                // Clamp and convert to short
                float clamped = Math.Max(-1.0f, Math.Min(1.0f, sample));
                short s = (short)(clamped * 32767);
                bw.Write(s);
            }
        }
    }
}
