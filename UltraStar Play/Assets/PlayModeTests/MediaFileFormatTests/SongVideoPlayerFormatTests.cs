using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongVideoPlayerFormatTests : AbstractMediaFileFormatTests
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("VideoFileFormatTests/avi-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-hvec-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/webm-vp8-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("VideoFileFormatTests/f4v-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/flv-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mkv-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mov-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-av1-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/wmv-TestSong.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFileName)
    {
        yield return SongVideoPlayerShouldLoadFile(txtFileName);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByThirdPartyLib))]
    public IEnumerator ShouldLoadViaThirdParty(string txtFileName)
    {
        yield return SongVideoPlayerShouldLoadFile(txtFileName);
    }
}
