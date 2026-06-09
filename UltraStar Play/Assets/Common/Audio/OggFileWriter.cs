using System.IO;
using UnityEngine;

public static class OggFileWriter
{
    private const float DefaultQuality = 0.7f;

    public static void WriteFile(string outputPath, AudioClip audioClip, float quality = DefaultQuality)
    {
        // OggVorbis.VorbisPlugin cannot handle special characters in output folder path.
        // Workaround: Save in ASCII-only path, then move
        string temporaryOutputPath = Path.GetTempFileName().Replace(".tmp", ".ogg");
        OggVorbis.VorbisPlugin.Save(temporaryOutputPath, audioClip, quality);
        FileUtils.MoveFileOverwriteIfExists(temporaryOutputPath, outputPath);
    }

    public static void WriteFile(string outputPath, int sampleRate, int channels, float[] samples,
        float quality = DefaultQuality)
    {
        AudioClip audioClip = AudioClip.Create("SaveOggAudioClip", samples.Length / channels, channels, sampleRate, false);
        audioClip.SetData(samples, 0);

        WriteFile(outputPath, audioClip, quality);

        Object.DestroyImmediate(audioClip);
    }
}
