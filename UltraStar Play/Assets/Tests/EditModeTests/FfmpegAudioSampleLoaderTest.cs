using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class FfmpegAudioSampleLoaderTest
{
    protected static readonly string mediaFileFormatTestFolderPath =
        $"{Application.dataPath}/Tests/PlayModeTests/MediaFileFormatTests/MediaFileFormatTestSongs";

    private FfmpegAudioSampleLoader ffmpegAudioSampleLoader;

    [SetUp]
    public void SetUp()
    {
        ffmpegAudioSampleLoader = new();
        ffmpegAudioSampleLoader.ConfigureFfmpeg(ApplicationUtils.GetStreamingAssetsPath("FfmpegLibraries/Windows"));
    }

    [Test]
    [TestCaseSource(nameof(GetAudioFileNames))]
    public async Task ShouldLoadAudioSamples(string fileName)
    {
        string filePath = $"{mediaFileFormatTestFolderPath}/{fileName}";
        FfmpegAudioSampleLoader.FfmpegAudioSamplesData ffmpegAudioSamplesData = ffmpegAudioSampleLoader.Load(filePath);
        Assert.NotNull(ffmpegAudioSamplesData);
        Assert.IsTrue(ffmpegAudioSamplesData.Channels is 1 or 2);
        Assert.Greater(ffmpegAudioSamplesData.SampleRate, 0);
        Assert.NotNull(ffmpegAudioSamplesData.Samples);
        Assert.Greater(ffmpegAudioSamplesData.Samples.Length, 0);

        if (ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(filePath)))
        {
            AudioClip audioClip = await AudioClipTestUtils.LoadAudioClipAsync(filePath);
            Assert.AreEqual(audioClip.channels, ffmpegAudioSamplesData.Channels);
            Assert.AreEqual(audioClip.frequency, ffmpegAudioSamplesData.SampleRate);
            double actualLengthInSeconds = (double)ffmpegAudioSamplesData.Samples.Length / ffmpegAudioSamplesData.Channels / ffmpegAudioSamplesData.SampleRate;
            Assert.AreEqual(audioClip.length, actualLengthInSeconds, 0.5);
            Object.DestroyImmediate(audioClip);
        }
    }

    public static IEnumerable<string> GetAudioFileNames()
    {
        string testAudioDir = mediaFileFormatTestFolderPath;
        return Directory.GetFiles(testAudioDir)
            .Where(it => ApplicationUtils.IsVlcSupportedAudioFormat(Path.GetExtension(it))
                         || ApplicationUtils.IsVlcSupportedVideoFormat(Path.GetExtension(it)))
            .Select(Path.GetFileName)
            .ToList();
    }
}
