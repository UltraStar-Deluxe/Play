using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FileFormatConversionTests : AbstractMediaFileFormatTests
{
    private static readonly string tempFolder = $"{Application.temporaryCachePath}/MediaFileConversionTest";

    private static readonly List<TestCaseData> fileNamesWithAudioSupportedByFfmpeg = new List<TestCaseData>()
    {
        new TestCaseData("aac-TestSong.txt").Returns(null),
        new TestCaseData("aiff-TestSong.txt").Returns(null),
        new TestCaseData("flac-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> fileNamesWithVideoSupportedByFfmpeg = new List<TestCaseData>()
    {
        new TestCaseData("f4v-TestSong.txt").Returns(null),
        new TestCaseData("flv-TestSong.txt").Returns(null),
        new TestCaseData("mkv-TestSong.txt").Returns(null),
        new TestCaseData("mov-TestSong.txt").Returns(null),
        new TestCaseData("mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("wmv-TestSong.txt").Returns(null),
    };

    [OneTimeSetUp]
    public void DeleteTempFolder()
    {
        DirectoryUtils.Delete(tempFolder, true);
    }

    [Test]
    public void ShouldReturnTargetFileNameFromFfmpegArguments()
    {
        string targetFileName = SongMediaFileConversionManager.GetTargetFileNameFromFfmpegArguments(
            "-y -i \"F:/Dev/UltraStar-Songs-Dev/Some Artist - Some Title/Some Artist - Some Title.mp4\" -c:v libvpx -c:a libvorbis \"F:/Dev/UltraStar-Songs-Dev/Some Artist - Some Title/Some Artist - Some Title-vp8.webm\"");
        Assert.AreEqual("Some Artist - Some Title-vp8.webm", targetFileName);

        string targetFileName2 = SongMediaFileConversionManager.GetTargetFileNameFromFfmpegArguments(
            "-y -i \"F:\\Dev\\UltraStar-Songs-Dev\\Some Artist - Some Title\\Some Artist - Some Title.mp4\" -c:v libvpx -c:a libvorbis \"F:\\Dev\\UltraStar-Songs-Dev\\Some Artist - Some Title\\Some Artist - Some Title-vp8.webm\"");
        Assert.AreEqual("Some Artist - Some Title-vp8.webm", targetFileName2);
    }

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithAudioSupportedByFfmpeg))]
    public IEnumerator ShouldConvertAudio(string txtFileName)
    {
        yield return ShouldConvertFile(txtFileName, audioFileFormatTestFolderPath, true);
    }

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithVideoSupportedByFfmpeg))]
    public IEnumerator ShouldConvertVideo(string txtFileName)
    {
        return ShouldConvertFile(txtFileName, videoFileFormatTestFolderPath, false);
    }

    private IEnumerator ShouldConvertFile(string txtFileName, string testFolderPath, bool isAudio)
    {
        LogAssert.ignoreFailingMessages = true;

        string songFilePath = GetSongMetaFilePath(txtFileName, testFolderPath);
        SongMeta songMeta = LoadSongMeta(songFilePath);
        string originalSourceFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio);

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
        int maxRetry = 3;
        long conversionStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        SongMediaFileConversionManager.MinTargetFileSizeInBytes = 10 * 1024; // 10 KB
        SongMediaFileConversionManager.Instance.ConvertFileToSupportedFormat(tempSourceFilePath,
            $"test media '{Path.GetFileName(tempSourceFilePath)}'",
            Translation.Of($"Convert '{Path.GetFileName(tempSourceFilePath)}' to supported format"),
            isAudio,
            true,
            maxRetry,
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
