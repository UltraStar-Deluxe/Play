using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongAudioPlayerFileFormatTests : AbstractMediaFileFormatTests
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("mp3-ConstantBitRate-TestSong.txt").Returns(null),
        new TestCaseData("mp3-VariableBitRate-TestSong.txt").Returns(null),
        new TestCaseData("ogg-TestSong.txt").Returns(null),
        new TestCaseData("wav-TestSong.txt").Returns(null),

        // Supported via Unity video player
        new TestCaseData("avi-TestSong.txt").Returns(null),
        new TestCaseData("mp4-TestSong.txt").Returns(null),
        new TestCaseData("mp4-hvec-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp8-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("aac-TestSong.txt").Returns(null),
        new TestCaseData("aiff-TestSong.txt").Returns(null),
        new TestCaseData("flac-TestSong.txt").Returns(null),
        new TestCaseData("m4a-TestSong.txt").Returns(null),
        new TestCaseData("wma-TestSong.txt").Returns(null),

        // Supported via third party video player
        new TestCaseData("f4v-TestSong.txt").Returns(null),
        new TestCaseData("flv-TestSong.txt").Returns(null),
        new TestCaseData("mkv-TestSong.txt").Returns(null),
        new TestCaseData("mov-TestSong.txt").Returns(null),
        new TestCaseData("mp4-av1-TestSong.txt").Returns(null),
        new TestCaseData("mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("wmv-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByMidiManager = new List<TestCaseData>()
    {
        new TestCaseData("midi-TestSong.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFile(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByThirdPartyLib))]
    public IEnumerator ShouldLoadViaThirdParty(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFile(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByMidiManager))]
    public IEnumerator ShouldLoadMidi(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFile(txtFilePath, 8000);
    }
}
