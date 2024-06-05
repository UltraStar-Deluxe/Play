using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongVideoPlayerFormatTest : AbstractMediaFileFormatTest
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("avi-TestSong.txt").Returns(null),
        new TestCaseData("mp4-TestSong.txt").Returns(null),
        new TestCaseData("mp4-hvec-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp8-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("f4v-TestSong.txt").Returns(null),
        new TestCaseData("flv-TestSong.txt").Returns(null),
        new TestCaseData("mkv-TestSong.txt").Returns(null),
        new TestCaseData("mov-TestSong.txt").Returns(null),
        new TestCaseData("mp4-av1-TestSong.txt").Returns(null),
        new TestCaseData("mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("wmv-TestSong.txt").Returns(null),

        // Mix of audio and video file formats
        new TestCaseData("flac-mkv-TestSong.txt").Returns(null),
        new TestCaseData("flac-webm-vp8-TestSong.txt").Returns(null),
        new TestCaseData("ogg-mkv-TestSong.txt").Returns(null),
        new TestCaseData("ogg-webm-vp8-TestSong.txt").Returns(null),
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
