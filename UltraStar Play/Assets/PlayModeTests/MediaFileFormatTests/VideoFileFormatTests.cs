using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class VideoFileFormatTests : AbstractMediaFileFormatTests
{
    private static readonly List<TestCaseData> fileNamesWithVideoSupportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("avi-TestSong.txt").Returns(null),
        new TestCaseData("mp4-TestSong.txt").Returns(null),
        new TestCaseData("mp4-av1-TestSong.txt").Returns(null),
        new TestCaseData("mp4-hvec-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp8-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> fileNamesWithVideoSupportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("f4v-TestSong.txt").Returns(null),
        new TestCaseData("flv-TestSong.txt").Returns(null),
        new TestCaseData("mkv-TestSong.txt").Returns(null),
        new TestCaseData("mov-TestSong.txt").Returns(null),
        new TestCaseData("mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("wmv-TestSong.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithVideoSupportedByUnity))]
    public IEnumerator ShouldLoadUnitySupportedVideo(string txtFileName)
    {
        yield return ShouldLoadVideoFile(txtFileName);
    }

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithVideoSupportedByThirdPartyLib))]
    public IEnumerator ShouldLoadThirdPartyLibSupportedVideo(string txtFileName)
    {
        yield return ShouldLoadVideoFile(txtFileName);
    }
}
