using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SupportedFileFormatListTest
{
    [Test]
    public void ShouldListSupportedAudioFormats()
    {
        List<string> expectedAudioFormats = new List<string>()
        {
            "aac",
            "aiff",
            "flac",
            "m4a",
            "mp3",
            "ogg",
            "wav",
            "wma",
        };
        List<string> supportedFormats = ApplicationUtils.unitySupportedAudioFiles.ToList();
        foreach (string expectedAudioFormat in expectedAudioFormats)
        {
            Assert.Contains(expectedAudioFormat, supportedFormats);
        }
    }

    [Test]
    public void ShouldListSupportedVideoFormats()
    {
        List<string> expectedVideoFormats = new List<string>()
        {
            "avi",
            "f4v",
            "flv",
            "mkv",
            "mov",
            "mp4",
            "mpg",
            "webm",
            "wmv",
        };
        List<string> supportedFormats = ApplicationUtils.unitySupportedVideoFiles.ToList();
        foreach (string expectedVideoFormat in expectedVideoFormats)
        {
            Assert.Contains(expectedVideoFormat, supportedFormats);
        }
    }
}
