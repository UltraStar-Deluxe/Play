using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class SongAudioPlayerFileFormatTest : AbstractMediaFileFormatTest
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("mp3-ConstantBitRate.txt", typeof(AudioSourceAudioSupportProvider)).Returns(null),
        new TestCaseData("mp3-VariableBitRate.txt", typeof(AudioSourceAudioSupportProvider)).Returns(null),
        new TestCaseData("ogg.txt", typeof(AudioSourceAudioSupportProvider)).Returns(null),
        new TestCaseData("wav.txt", typeof(AudioSourceAudioSupportProvider)).Returns(null),

        // Video formats, supported via Unity VideoPlayer
        new TestCaseData("avi.txt", typeof(VideoPlayerAudioSupportProvider)).Returns(null),
        new TestCaseData("mp4.txt", typeof(VideoPlayerAudioSupportProvider)).Returns(null),
        new TestCaseData("mp4-hvec.txt", typeof(VideoPlayerAudioSupportProvider)).Returns(null),
        new TestCaseData("webm-vp8.txt", typeof(VideoPlayerAudioSupportProvider)).Returns(null),
    };

    private static readonly List<TestCaseData> supportedByVlc = new List<TestCaseData>()
    {
        new TestCaseData("aac.txt").Returns(null),
        new TestCaseData("aiff.txt").Returns(null),
        new TestCaseData("flac.txt").Returns(null),
        new TestCaseData("m4a.txt").Returns(null),
        new TestCaseData("wma.txt").Returns(null),

        // Video formats
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

        // Video formats
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
    public IEnumerator ShouldLoadViaUnity(string txtFilePath, Type expectedAudioSupportProviderType)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, expectedAudioSupportProviderType);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(VlcAudioSupportProvider));
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(AvproAudioSupportProvider));
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByMidiManager))]
    public IEnumerator ShouldLoadMidi(string txtFilePath)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(MidiAudioSupportProvider), 8000);
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaVlcAsFallback()
    {
        // AVPro does not support aiff.
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.WhenUnsupportedByUnity;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.WhenUnsupportedByUnity;
        yield return SongAudioPlayerShouldLoadFileAsync("aiff.txt", typeof(VlcAudioSupportProvider));
        
        // Unity does not support flac format, but AVPro would support it.
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        yield return SongAudioPlayerShouldLoadFileAsync("flac.txt", typeof(VlcAudioSupportProvider));
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaAvproAsFallback()
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.WhenUnsupportedByUnity;
        yield return SongAudioPlayerShouldLoadFileAsync("flac.txt", typeof(AvproAudioSupportProvider));
    }
}
