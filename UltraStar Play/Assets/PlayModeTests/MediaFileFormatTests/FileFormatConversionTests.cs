using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FileFormatConversionTests : AbstractMediaFileFormatTests
{
    private static readonly string tempFolder = $"{Application.temporaryCachePath}/MediaFileConversionTest";

    [OneTimeSetUp]
    public void DeleteTempFolder()
    {
        DirectoryUtils.Delete(tempFolder, true);
    }

    [Test]
    public void GetTargetFileNameFromFfmpegArgumentsTest()
    {
        string targetFileName = SongMediaFileConversionManager.GetTargetFileNameFromFfmpegArguments(
            "-y -i \"F:/Dev/UltraStar-Songs-Dev/Some Artist - Some Title/Some Artist - Some Title.mp4\" -c:v libvpx -c:a libvorbis \"F:/Dev/UltraStar-Songs-Dev/Some Artist - Some Title/Some Artist - Some Title-vp8.webm\"");
        Assert.AreEqual("Some Artist - Some Title-vp8.webm", targetFileName);

        string targetFileName2 = SongMediaFileConversionManager.GetTargetFileNameFromFfmpegArguments(
            "-y -i \"F:\\Dev\\UltraStar-Songs-Dev\\Some Artist - Some Title\\Some Artist - Some Title.mp4\" -c:v libvpx -c:a libvorbis \"F:\\Dev\\UltraStar-Songs-Dev\\Some Artist - Some Title\\Some Artist - Some Title-vp8.webm\"");
        Assert.AreEqual("Some Artist - Some Title-vp8.webm", targetFileName2);
    }

    /////////////////////////////////////////////////////////
    // common audio formats supported by ffmpeg
    /////////////////////////////////////////////////////////
    [UnityTest]
    public IEnumerator AacTest()
    {
        return AudioFileConversionTest("aac-");
    }

    [UnityTest]
    public IEnumerator AiffTest()
    {
        return AudioFileConversionTest("aiff-");
    }

    [UnityTest]
    public IEnumerator FlacTest()
    {
        return AudioFileConversionTest("flac-");
    }

    /////////////////////////////////////////////////////////
    // common video formats supported by ffmpeg
    /////////////////////////////////////////////////////////
    [UnityTest]
    public IEnumerator F4vConversionTest()
    {
        return VideoFileConversionTest("f4v-");
    }

    [UnityTest]
    public IEnumerator FlvConversionTest()
    {
        return VideoFileConversionTest("flv-");
    }

    [UnityTest]
    public IEnumerator MkvConversionTest()
    {
        return VideoFileConversionTest("mkv-");
    }

    [UnityTest]
    public IEnumerator MovConversionTest()
    {
        return VideoFileConversionTest("mov-");
    }

    [UnityTest]
    public IEnumerator Mpeg2ConversionTest()
    {
        return VideoFileConversionTest("mpeg2-");
    }

    [UnityTest]
    public IEnumerator WebVp9ConversionTest()
    {
        return VideoFileConversionTest("webm-vp9-");
    }

    [UnityTest]
    public IEnumerator WmvConversionTest()
    {
        return VideoFileConversionTest("wmv-");
    }

    private IEnumerator VideoFileConversionTest(string songFilePrefix)
    {
        return FileConversionTest(songFilePrefix, videoFileFormatTestFolderPath, "webm", false);
    }

    private IEnumerator AudioFileConversionTest(string songFilePrefix)
    {
        return FileConversionTest(songFilePrefix, audioFileFormatTestFolderPath, "ogg", true);
    }

    private IEnumerator FileConversionTest(string songFilePrefix, string testFolderPath, string targetFileExtension, bool isAudio)
    {
        LogAssert.ignoreFailingMessages = true;

        string songFilePath = GetSongMetaFilePath(songFilePrefix, testFolderPath);
        SongMeta songMeta = LoadSongMeta(songFilePath);
        string originalSourceFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Mp3);

        // Copy file to temp folder so that we don't modify the original file
        string tempSourceFilePath = $"{tempFolder}/{Path.GetFileName(originalSourceFilePath)}";
        FileUtils.Copy(originalSourceFilePath, tempSourceFilePath, true);

        long copyStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        while (!File.Exists(tempSourceFilePath))
        {
            if (TimeUtils.IsDurationAboveThresholdInMillis(copyStartTimeInMillis, 1000))
            {
                Assert.Fail($"Failed to copy file {originalSourceFilePath} to {tempSourceFilePath}");
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        bool isSuccessful = false;
        bool ignoreEqualFileExtension = true;
        long conversionStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        SongMediaFileConversionManager.MinTargetFileSizeInBytes = 10 * 1024; // 10 KB
        SongMediaFileConversionManager.Instance.ConvertFileToSupportedFormat(tempSourceFilePath,
            $"test media '{Path.GetFileName(tempSourceFilePath)}'",
            $"Convert '{Path.GetFileName(tempSourceFilePath)}' to supported format",
            isAudio,
            ignoreEqualFileExtension,
            3,
            targetFilePath =>
            {
                if (!File.Exists(targetFilePath))
                {
                    Assert.Fail($"Failed to convert '{tempSourceFilePath}' to supported format. Target file '{targetFilePath}' does not exist");
                    return;
                }

                isSuccessful = true;
                long durationInMillis = TimeUtils.GetUnixTimeMilliseconds() - conversionStartTimeInMillis;
                Debug.Log($"Successfully converted '{originalSourceFilePath}' to '{Path.GetFileName(targetFilePath)}' in {durationInMillis} ms");
            },
            conversionError =>
            {
                Assert.Fail($"Failed to convert '{tempSourceFilePath}' to supported format. Conversion error: {conversionError.ErrorMessage}");
            });

        long maxWaitTimeInMillis = 3000;
        yield return new WaitUntil(() => isSuccessful
                                         || TimeUtils.IsDurationAboveThresholdInMillis(conversionStartTimeInMillis, maxWaitTimeInMillis));

        if (!isSuccessful)
        {
            Assert.Fail($"Failed to convert '{originalSourceFilePath}' to supported format within {maxWaitTimeInMillis} ms");
        }
    }
}
