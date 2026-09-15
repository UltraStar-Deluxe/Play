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

    [UnityTest]
    [TestCaseSource(nameof(supportedByUnity))]
    public IEnumerator ShouldLoadViaUnity(string txtFileName, Type expectedVideoSupportProviderType)
    {
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongVideoPlayerShouldLoadFileAsync(txtFileName, expectedVideoSupportProviderType);
    }
    
    private static void ConfigureMediaApiSettings(EApiUsage unity, EApiUsage avpro, EApiUsage vlc)
    {
        SettingsManager.Instance.Settings.UnityMediaApiUsage = unity;
        SettingsManager.Instance.Settings.AvProApiUsage = avpro;
        SettingsManager.Instance.Settings.VlcApiUsage = vlc;
    }
}
