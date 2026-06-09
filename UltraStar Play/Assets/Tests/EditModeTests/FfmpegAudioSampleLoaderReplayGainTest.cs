using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class FfmpegAudioSampleLoaderReplayGainTest
{
    private const string TestFilesPath =
        @"C:\Dev\Projects\GitHub\achimmihca\MelodyMania\UltraStar Play\Assets\Tests\EditModeTests\ReplayGain";

    private const string ReplayGainOffFile = "ReplayGain-off.ogg";
    private const string ReplayGainOnFile = "ReplayGain-on.ogg";

    private FfmpegAudioSampleLoader ffmpegAudioSampleLoader;

    [SetUp]
    public void SetUp()
    {
        ffmpegAudioSampleLoader = new();
        ffmpegAudioSampleLoader.ConfigureFfmpeg(ApplicationUtils.GetStreamingAssetsPath("FfmpegLibraries/Windows"));
    }

    [Test]
    public void ReplayGainOnShouldBeSignificantlyMoreSilent()
    {
        // When
        FfmpegAudioSampleLoader.FfmpegAudioSamplesData dataOff =
            ffmpegAudioSampleLoader.Load(Path.Combine(TestFilesPath, ReplayGainOffFile));
        FfmpegAudioSampleLoader.FfmpegAudioSamplesData dataOn = ffmpegAudioSampleLoader.Load(
            Path.Combine(TestFilesPath, ReplayGainOnFile),
            replayGainMode: FfmpegAudioSampleLoader.ReplayGainMode.Track);
        Assert.AreEqual(dataOff.Samples.Length, dataOn.Samples.Length, "Sample count mismatch");

        // Then
        double sumAbsOff = dataOff.Samples.Select(Math.Abs).Sum();
        double sumAbsOn = dataOn.Samples.Select(Math.Abs).Sum();
        Assert.Less(sumAbsOn, sumAbsOff * 0.5, "ReplayGain-on should be significantly more silent (factor < 0.5)");
        for (int i = 0; i < dataOff.Samples.Length; i++)
        {
            Assert.LessOrEqual(Math.Abs(dataOn.Samples[i]), Math.Abs(dataOff.Samples[i]) + 0.0001f);
        }
    }

    [Test]
    public void ReplayGainOffShouldBeSameVolume()
    {
        FfmpegAudioSampleLoader.FfmpegAudioSamplesData dataOff = ffmpegAudioSampleLoader.Load(
            Path.Combine(TestFilesPath, ReplayGainOnFile), replayGainMode: FfmpegAudioSampleLoader.ReplayGainMode.Off);
        FfmpegAudioSampleLoader.FfmpegAudioSamplesData dataReference =
            ffmpegAudioSampleLoader.Load(Path.Combine(TestFilesPath, ReplayGainOffFile),
                replayGainMode: FfmpegAudioSampleLoader.ReplayGainMode.Off);

        Assert.AreEqual(dataReference.Samples.Length, dataOff.Samples.Length, "Sample count mismatch");
        for (int i = 0; i < dataReference.Samples.Length; i++)
        {
            Assert.AreEqual(dataReference.Samples[i], dataOff.Samples[i], 0.0001f, $"Sample {i} mismatch");
        }
    }
}
