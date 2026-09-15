using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WebViewTest : AbstractMediaFileFormatTest
{
    private const long WebViewMaxWaitTimeInMillis = 30000;
    private const double WebViewTargetDurationInMillis = 242561;

    private static string[] shouldUseLocalAudioFiles = new string[]
    {
        "WebViewTests/AudioUrlAndExistingAudio.txt",
    };

    private static string[] shouldUseWebViewFiles = new string[]
    {
        "WebViewTests/AudioUrlAndMissingAudio.txt",
        "WebViewTests/AudioUrlOnly.txt",
        "WebViewTests/AudioOnly.txt",
        "WebViewTests/VideoUrlOnly.txt",
        "WebViewTests/WebsiteOnly.txt",
        "WebViewTests/UsdbVideoTagSyntax.txt",
    };

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        settings.AcceptedWebViewHosts = new List<string>()
        {
            "youtube.com",
        };
    }

    [UnityTest]
    public IEnumerator ShouldUseLocalAudioTest([ValueSource(nameof(shouldUseLocalAudioFiles))] string txtFilePath)
    {
        LogAssertUtils.IgnoreFailingMessages();
        SettingsManager.Instance.Settings.UnityMediaApiUsage = EApiUsage.Preferred;
        yield return SongAudioPlayerShouldLoadFileAsync(
            txtFilePath,
            typeof(AudioSourceAudioSupportProvider));
    }
}
