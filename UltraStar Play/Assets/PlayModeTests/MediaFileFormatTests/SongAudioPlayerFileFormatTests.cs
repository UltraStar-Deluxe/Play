using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongAudioPlayerFileFormatTests : AbstractMediaFileFormatTests
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("AudioFileFormatTests/mp3-ConstantBitRate-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/mp3-VariableBitRate-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/ogg-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/wav-TestSong.txt").Returns(null),

        // Supported via Unity video player
        new TestCaseData("VideoFileFormatTests/avi-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-hvec-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/webm-vp8-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("AudioFileFormatTests/aac-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/aiff-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/flac-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/m4a-TestSong.txt").Returns(null),
        new TestCaseData("AudioFileFormatTests/wma-TestSong.txt").Returns(null),

        // Supported via third party video player
        new TestCaseData("VideoFileFormatTests/f4v-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/flv-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mkv-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mov-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mp4-av1-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/mpeg2-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/webm-vp9-TestSong.txt").Returns(null),
        new TestCaseData("VideoFileFormatTests/wmv-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByMidiManager = new List<TestCaseData>()
    {
        new TestCaseData("AudioFileFormatTests/midi-TestSong.txt").Returns(null),
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
