using System;
using System.IO;
using OggVorbisEncoder;
using UnityEngine;

public static class OggFileWriter
{
    public static void WriteFile(string outputPath, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        WriteFile(outputPath, clip.frequency, clip.channels, samples);
    }

    public static void WriteFile(string outputPath, int frequency, int channels, float[] samples)
    {
        if (!outputPath.ToLowerInvariant().EndsWith(".ogg", StringComparison.InvariantCulture))
        {
            outputPath += ".ogg";
        }

        string directoryPath = Path.GetDirectoryName(outputPath);
        if (!directoryPath.IsNullOrEmpty() && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // The OggVorbisEncoder works with float[][] for channels
        float[][] pcmData = new float[channels][];
        int samplesPerChannel = samples.Length / channels;
        for (int ch = 0; ch < channels; ch++)
        {
            pcmData[ch] = new float[samplesPerChannel];
            for (int i = 0; i < samplesPerChannel; i++)
            {
                pcmData[ch][i] = samples[i * channels + ch];
            }
        }

        using FileStream fileStream = new FileStream(outputPath, FileMode.Create);
        Encode(fileStream, frequency, channels, pcmData);
    }

    private static void Encode(Stream outputStream, int frequency, int channels, float[][] pcmData)
    {
        // Based on OggVorbisEncoder examples
        // See https://github.com/Keyous/OggVorbisEncoder
        
        const float baseQuality = 0.4f; // approx 128kbps for stereo
        
        VorbisInfo info = VorbisInfo.InitVariableBitRate(channels, frequency, baseQuality);

        // Serialization setup
        ProcessingState processingState = ProcessingState.Create(info);
        
        // Use a random serial number for the stream
        int serialNumber = new System.Random().Next();
        OggStream oggStream = new OggStream(serialNumber);

        // 1. Write the header
        // Vorbis headers are 3 packets: Identification, Comments, Setup
        oggStream.PacketIn(HeaderPacketBuilder.BuildInfoPacket(info));
        oggStream.PacketIn(HeaderPacketBuilder.BuildCommentsPacket(new Comments()));
        oggStream.PacketIn(HeaderPacketBuilder.BuildBooksPacket(info));
        FlushPages(oggStream, outputStream, false);

        // 2. Encode PCM data
        int samplesPerChannel = pcmData[0].Length;
        processingState.WriteData(pcmData, samplesPerChannel);
        processingState.WriteEndOfStream();

        // 3. Write encoded packets
        while (processingState.PacketOut(out OggPacket packet))
        {
            oggStream.PacketIn(packet);
            FlushPages(oggStream, outputStream, false);
        }
        
        FlushPages(oggStream, outputStream, true);
    }

    private static void FlushPages(OggStream oggStream, Stream outputStream, bool force)
    {
        while (oggStream.PageOut(out OggPage page, force))
        {
            outputStream.Write(page.Header, 0, page.Header.Length);
            outputStream.Write(page.Body, 0, page.Body.Length);
        }
    }
}
