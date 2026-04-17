using UnityEngine;

public static class OggFileWriter
{
    private const float DefaultQuality = 0.7f;

    public static void WriteFile(string outputPath, AudioClip audioClip, float quality = DefaultQuality)
    {
        OggVorbis.VorbisPlugin.Save(outputPath, audioClip, quality);
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
