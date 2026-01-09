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

    private static readonly List<TestCaseData> supportedByVlc = new List<TestCaseData>()
    {
        new TestCaseData("aac.txt").Returns(null),
        new TestCaseData("aiff.txt").Returns(null),
        new TestCaseData("flac.txt").Returns(null),
        new TestCaseData("m4a.txt").Returns(null),
        new TestCaseData("wma.txt").Returns(null),

        // Supported via video player
        new TestCaseData("f4v.txt").Returns(null),
        new TestCaseData("flv.txt").Returns(null),
        new TestCaseData("mkv.txt").Returns(null),
        new TestCaseData("mov.txt").Returns(null),
        new TestCaseData("mp4-av1.txt").Returns(null),
        new TestCaseData("mpeg2.txt").Returns(null),
        new TestCaseData("webm-vp9.txt").Returns(null),
        new TestCaseData("wmv.txt").Returns(null),
    };

    private static readonly List<TestCaseData> supportedByAvpro = new List<TestCaseData>()
    {
        new TestCaseData("aac.txt").Returns(null),
        // new TestCaseData("aiff.txt").Returns(null), // Not supported by AVPro
        new TestCaseData("flac.txt").Returns(null),
        new TestCaseData("m4a.txt").Returns(null),
        new TestCaseData("wma.txt").Returns(null),

        // Supported via video player
        // new TestCaseData("f4v.txt").Returns(null), // Not supported by AVPro
        // new TestCaseData("flv.txt").Returns(null), // Not supported by AVPro
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
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByMidiManager))]
    public IEnumerator ShouldLoadMidi(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, 8000);
    }
}
