using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongAudioPlayerFileFormatTest : AbstractMediaFileFormatTest
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("mp3-ConstantBitRate.txt").Returns(null),
        new TestCaseData("mp3-VariableBitRate.txt").Returns(null),
        new TestCaseData("ogg.txt").Returns(null),
        new TestCaseData("wav.txt").Returns(null),

        // Supported via Unity video player
        new TestCaseData("avi.txt").Returns(null),
        new TestCaseData("mp4.txt").Returns(null),
        new TestCaseData("mp4-hvec.txt").Returns(null),
        new TestCaseData("webm-vp8.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("aac.txt").Returns(null),
        new TestCaseData("aiff.txt").Returns(null),
        new TestCaseData("flac.txt").Returns(null),
        new TestCaseData("m4a.txt").Returns(null),
        new TestCaseData("wma.txt").Returns(null),

        // Supported via third party video player
        new TestCaseData("f4v.txt").Returns(null),
        // new TestCaseData("flv.txt").Returns(null), // TODO: The test for flv sometimes fails. But seems to work in game.
        new TestCaseData("mkv.txt").Returns(null),
        new TestCaseData("mov.txt").Returns(null),
        new TestCaseData("mp4-av1.txt").Returns(null),
        new TestCaseData("mpeg2.txt").Returns(null),
        new TestCaseData("webm-vp9.txt").Returns(null),
        new TestCaseData("wmv.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByMidiManager = new List<TestCaseData>()
    {
        new TestCaseData("midi.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByThirdPartyLib))]
    public IEnumerator ShouldLoadViaThirdParty(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByMidiManager))]
    public IEnumerator ShouldLoadMidi(string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, 8000);
    }
}
