using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

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
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }

    [UnityTest]
    [TestCaseSource(nameof(supportedByVlc))]
    public IEnumerator ShouldLoadViaVlc(string txtFileName, Type expectedVideoSupportProviderType)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }
    
    [UnityTest]
    [TestCaseSource(nameof(supportedByAvpro))]
    public IEnumerator ShouldLoadViaAvpro(string txtFileName, Type expectedVideoSupportProviderType)
    {
        SettingsManager.Instance.Settings.VlcToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Never;
        SettingsManager.Instance.Settings.AvProToPlayMediaFilesUsage = EThirdPartyLibraryUsage.Always;
        
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }
}
