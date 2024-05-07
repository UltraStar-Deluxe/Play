using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class AudioFileFormatTests : AbstractMediaFileFormatTests
{
    private static readonly List<TestCaseData> fileNamesWithAudioSupportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("mp3-ConstantBitRate-TestSong.txt").Returns(null),
        new TestCaseData("mp3-VariableBitRate-TestSong.txt").Returns(null),
        new TestCaseData("ogg-TestSong.txt").Returns(null),
        new TestCaseData("wav-TestSong.txt").Returns(null),
    };

    private static readonly List<TestCaseData> fileNamesWithAudioSupportedByThirdPartyLib = new List<TestCaseData>()
    {
        new TestCaseData("aac-TestSong.txt").Returns(null),
        new TestCaseData("aiff-TestSong.txt").Returns(null),
        new TestCaseData("flac-TestSong.txt").Returns(null),
        new TestCaseData("m4a-TestSong.txt").Returns(null),
        new TestCaseData("wma-TestSong.txt").Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithAudioSupportedByUnity))]
    public IEnumerator ShouldLoadUnitySupportedAudio(string txtFileName)
    {
        yield return ShouldLoadAudioFile(txtFileName);
    }

    [UnityTest]
    [TestCaseSource(nameof(fileNamesWithAudioSupportedByThirdPartyLib))]
    public IEnumerator ShouldLoadThirdPartyLibSupportedAudio(string txtFileName)
    {
        yield return ShouldLoadAudioFile(txtFileName);
    }
}
