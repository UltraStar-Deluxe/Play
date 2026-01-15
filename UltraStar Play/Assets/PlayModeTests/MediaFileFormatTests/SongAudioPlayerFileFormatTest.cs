using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

/**
 * Tests the audio player support, including audio support provider selection.
 * Therefore, it assumes the default provider priority (when all are enabled): Unity > AVPro > VLC.
 */
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
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, expectedAudioSupportProviderType);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFilePath)
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Enabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(AvproAudioSupportProvider));
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFilePath)
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Disabled, EApiUsage.Enabled);
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(VlcAudioSupportProvider));
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByMidiManager))]
    public IEnumerator ShouldLoadMidi(string txtFilePath)
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(txtFilePath, typeof(MidiAudioSupportProvider), 8000);
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaVlcAsFallback()
    {
        // Neither Unity nor AVPro does support aiff.
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Enabled, EApiUsage.Enabled);
        yield return SongAudioPlayerShouldLoadFileAsync("aiff.txt", typeof(VlcAudioSupportProvider));
        
        // Unity does not support flac format, but AVPro does support it. So must disable AVPro to test VLC fallback.
        SettingsManager.Instance.Settings.AvProApiUsage = EApiUsage.Disabled;
        yield return SongAudioPlayerShouldLoadFileAsync("flac.txt", typeof(VlcAudioSupportProvider));
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaAvproAsFallback()
    {
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Enabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync("flac.txt", typeof(AvproAudioSupportProvider));
    }
    
    private static void ConfigureMediaApiSettings(EApiUsage unity, EApiUsage avpro, EApiUsage vlc)
    {
        SettingsManager.Instance.Settings.UnityMediaApiUsage = unity;
        SettingsManager.Instance.Settings.AvProApiUsage = avpro;
        SettingsManager.Instance.Settings.VlcApiUsage = vlc;
    }
}
