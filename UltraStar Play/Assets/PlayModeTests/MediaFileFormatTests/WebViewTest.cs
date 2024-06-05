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
        "WebViewTests/AudioUrlAndExistingAudio-TestSong.txt",
    };

    private static string[] shouldUseWebViewFiles = new string[]
    {
        "WebViewTests/AudioUrlAndMissingAudio-TestSong.txt",
        "WebViewTests/AudioUrlOnly-TestSong.txt",
        "WebViewTests/AudioOnly-TestSong.txt",
        "WebViewTests/VideoUrlOnly-TestSong.txt",
        "WebViewTests/WebsiteOnly-TestSong.txt",
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
        yield return SongAudioPlayerShouldLoadFile(txtFilePath);
        Assert.IsFalse(SongAudioPlayer.CurrentAudioSupportProvider is WebViewAudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator ShouldUseWebView([ValueSource(nameof(shouldUseWebViewFiles))] string txtFilePath)
    {
        yield return SongAudioPlayerShouldLoadFile(txtFilePath, WebViewTargetDurationInMillis, WebViewMaxWaitTimeInMillis);
        Assert.IsTrue(SongAudioPlayer.CurrentAudioSupportProvider is WebViewAudioSupportProvider);
    }
}
