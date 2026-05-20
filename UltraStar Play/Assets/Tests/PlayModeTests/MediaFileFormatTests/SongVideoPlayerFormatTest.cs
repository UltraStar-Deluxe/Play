using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

/**
 * Tests the video player support, including video support provider selection.
 * Therefore, it assumes the default provider priority (when all are enabled): Unity > AVPro > VLC.
 */
public class SongVideoPlayerFormatTest : AbstractMediaFileFormatTest
{
    private static readonly List<TestCaseData> supportedByUnity = new List<TestCaseData>()
    {
        new TestCaseData("avi.txt", typeof(VideoPlayerVideoSupportProvider)).Returns(null),
        new TestCaseData("mp4.txt", typeof(VideoPlayerVideoSupportProvider)).Returns(null),
        new TestCaseData("mp4-hvec.txt", typeof(VideoPlayerVideoSupportProvider)).Returns(null),
        new TestCaseData("webm-vp8.txt", typeof(VideoPlayerVideoSupportProvider)).Returns(null),
    };

    private static readonly List<TestCaseData> supportedByVlc = new List<TestCaseData>()
    {
        new TestCaseData("f4v.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("flv.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("mkv.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("mov.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("mp4-av1.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("mpeg2.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("webm-vp9.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),
        new TestCaseData("wmv.txt", typeof(SongAudioPlayerVlcVideoSupportProvider)).Returns(null),

        // Mix of audio and video file formats
        new TestCaseData("flac-mkv.txt", typeof(VlcVideoSupportProvider)).Returns(null),
        new TestCaseData("flac-webm-vp8.txt", typeof(VlcVideoSupportProvider)).Returns(null),
        new TestCaseData("ogg-mkv.txt", typeof(VlcVideoSupportProvider)).Returns(null),
        new TestCaseData("ogg-webm-vp8.txt", typeof(VlcVideoSupportProvider)).Returns(null),
    };

    private static readonly List<TestCaseData> supportedByAvpro = new List<TestCaseData>()
    {
        // f4v and flv are not supported by AVPro
        // new TestCaseData("f4v.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        // new TestCaseData("flv.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("mkv.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("mov.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("mp4-av1.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("mpeg2.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("webm-vp9.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),
        new TestCaseData("wmv.txt", typeof(SongAudioPlayerAvproVideoSupportProvider)).Returns(null),

        // Mix of audio and video file formats
        new TestCaseData("flac-mkv.txt", typeof(AvproVideoSupportProvider)).Returns(null),
        new TestCaseData("flac-webm-vp8.txt", typeof(AvproVideoSupportProvider)).Returns(null),
        new TestCaseData("ogg-mkv.txt", typeof(AvproVideoSupportProvider)).Returns(null),
        new TestCaseData("ogg-webm-vp8.txt", typeof(AvproVideoSupportProvider)).Returns(null),
    };

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFileName, Type expectedVideoSupportProviderType)
    {
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFileName, Type expectedVideoSupportProviderType)
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Enabled, EApiUsage.Disabled);        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFileName, Type expectedVideoSupportProviderType)
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Disabled, EApiUsage.Enabled);
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }

    [UnityTest]
    public IEnumerator ShouldLoadViaVlcAsFallback()
    {
        // Neither Unity nor AVPro support f4v.
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Enabled, EApiUsage.Enabled);
        yield return SongVideoPlayerShouldLoadFileAsync("f4v.txt", typeof(SongAudioPlayerVlcVideoSupportProvider));
        
        // Unity does not support AV1 video format, but AVPro does support it. So must disable AVPro to test VLC fallback.
        SettingsManager.Instance.Settings.AvProApiUsage = EApiUsage.Disabled;
        yield return SongVideoPlayerShouldLoadFileAsync("webm-vp9.txt", typeof(SongAudioPlayerVlcVideoSupportProvider));
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaAvproAsFallback()
    {
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Enabled, EApiUsage.Disabled);
        yield return SongVideoPlayerShouldLoadFileAsync("webm-vp9.txt", typeof(SongAudioPlayerAvproVideoSupportProvider));
    }
    
    private static void ConfigureMediaApiSettings(EApiUsage unity, EApiUsage avpro, EApiUsage vlc)
    {
        SettingsManager.Instance.Settings.UnityMediaApiUsage = unity;
        SettingsManager.Instance.Settings.AvProApiUsage = avpro;
        SettingsManager.Instance.Settings.VlcApiUsage = vlc;
    }
}
