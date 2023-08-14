using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SupportedFileFormatTest
{
    [Test]
    public void FfmpegSupportedAudioFormatsTest()
    {
        List<string> expectedAudioFormats = new List<string>()
        {
            "mp3",
            "ogg",
            "wav",
            "flac",
            "aiff",
            "aac",
            "m4a",
            "wma",
        };
        List<string> supportedFormats = ApplicationUtils.ffmpegSupportedAudioFiles.ToList();
        foreach (string expectedAudioFormat in expectedAudioFormats)
        {
            Assert.Contains(expectedAudioFormat, supportedFormats);
        }
    }

    [Test]
    public void FfmpegSupportedVideoFormatsTest()
    {
        List<string> expectedVideoFormats = new List<string>()
        {
            "webm",
            "mp4",
            "mov",
            "wmv",
            "avi",
            "mkv",
            "flv",
        };
        List<string> supportedFormats = ApplicationUtils.ffmpegSupportedVideoFiles.ToList();
        foreach (string expectedVideoFormat in expectedVideoFormats)
        {
            Assert.Contains(expectedVideoFormat, supportedFormats);
        }
    }
}
