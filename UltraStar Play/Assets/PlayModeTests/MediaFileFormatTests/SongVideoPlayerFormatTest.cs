using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongVideoPlayerFormatTest : AbstractMediaFileFormatTest
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("avi.txt").Returns(null),
        new TestCaseData("mp4.txt").Returns(null),
        new TestCaseData("mp4-hvec.txt").Returns(null),
        new TestCaseData("webm-vp8.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("f4v.txt").Returns(null),
        // new TestCaseData("flv.txt").Returns(null), // TODO: The test for flv sometimes fails. But seems to work in game.
        new TestCaseData("mkv.txt").Returns(null),
        new TestCaseData("mov.txt").Returns(null),
        new TestCaseData("mp4-av1.txt").Returns(null),
        new TestCaseData("mpeg2.txt").Returns(null),
        new TestCaseData("webm-vp9.txt").Returns(null),
        new TestCaseData("wmv.txt").Returns(null),

        // Mix of audio and video file formats
        new TestCaseData("flac-mkv.txt").Returns(null),
        new TestCaseData("flac-webm-vp8.txt").Returns(null),
        new TestCaseData("ogg-mkv.txt").Returns(null),
        new TestCaseData("ogg-webm-vp8.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFileName)
    {
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName);
    }

    [Ignore("Requires third-party lib")]
    [UnityTest]
    [TestCaseSource(nameof(supportedByThirdPartyLib))]
    public IEnumerator ShouldLoadViaThirdParty(string txtFileName)
    {
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName);
    }
}
