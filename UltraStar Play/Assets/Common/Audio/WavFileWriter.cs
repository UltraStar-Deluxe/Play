using System;
using System.IO;
using UnityEngine;

// See http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
public static class WavFileWriter
{
    private const int HeaderSize = 44;
    private const int RescaleFactor = 32767;
    private const int BitsPerSample = 16;
    private const int BytesPerSample = 2;
    private const int EmptyByte = new();

    public static void WriteFile(string outputPath, AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        WriteFile(outputPath, clip.frequency, clip.channels, samples);
    }

    public static void WriteFile(string outputPath, int frequency, int channels, float[] samples)
    {
        if (!outputPath.ToLower().EndsWith(".wav"))
        {
            outputPath += ".wav";
        }

        string directoryPath = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // The steam will be closed, thanks to the using statement.
        using FileStream fileStream = CreateEmptyWavFile(outputPath);
        ConvertAndWrite(fileStream, samples);
        WriteHeader(fileStream, frequency, channels, samples.Length);
    }

    private static FileStream CreateEmptyWavFile(string filepath)
    {
        FileStream fileStream = new(filepath, FileMode.Create);
        for (int i = 0; i < HeaderSize; i++)
        {
            fileStream.WriteByte(EmptyByte);
        }

        return fileStream;
    }

    private static void ConvertAndWrite(FileStream fileStream, float[] samples)
    {
        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * RescaleFactor);
            Byte[] byteArray = BitConverter.GetBytes(intData[i]);
            byteArray.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fileStream, int frequency, int channels, int sampleCount)
    {
        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        Byte[] audioFormat = BitConverter.GetBytes((UInt16) 1);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(frequency);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(frequency * channels * BytesPerSample);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        Byte[] bitsPerSampleByteArray = BitConverter.GetBytes(BitsPerSample);
        fileStream.Write(bitsPerSampleByteArray, 0, 2);

        Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
