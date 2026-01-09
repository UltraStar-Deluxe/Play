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

    private static readonly List<TestCaseData> supportedByVlc = new List<TestCaseData>()
    {
        new TestCaseData("f4v.txt").Returns(null),
        new TestCaseData("flv.txt").Returns(null),
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

    private static readonly List<TestCaseData> supportedByAvpro = new List<TestCaseData>()
    {
        // new TestCaseData("f4v.txt").Returns(null), // Not supported by AVPro
        // new TestCaseData("flv.txt").Returns(null), // Not supported by AVPro
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
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFileName)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFileName)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName);
    }
}
