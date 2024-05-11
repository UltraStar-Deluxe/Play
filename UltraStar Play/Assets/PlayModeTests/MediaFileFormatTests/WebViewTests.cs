using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WebViewTests : AbstractMediaFileFormatTests
{
    private static string[] shouldUseLocalAudioFiles = new string[]
    {
        "AudioUrlAndExistingAudio-TestSong.txt",
    };

    private static string[] shouldUseWebViewFiles = new string[]
    {
        "AudioUrlAndMissingAudio-TestSong.txt",
        "AudioUrlOnly-TestSong.txt",
        "AudioOnly-TestSong.txt",
        "VideoUrlOnly-TestSong.txt",
        "WebsiteOnly-TestSong.txt",
    };

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        settings.AcceptedWebViewHosts = new List<string>()
        {
            "youtube.com",
        };
    }

    [UnityTest]
    public IEnumerator ShouldUseLocalAudioTest([ValueSource(nameof(shouldUseLocalAudioFiles))] string filePrefix)
    {
        yield return WebViewFileTest(filePrefix, localFileTargetDurationInMillis);
        Assert.AreNotEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator ShouldUseWebView([ValueSource(nameof(shouldUseWebViewFiles))] string filePrefix)
    {
        yield return WebViewFileTest(filePrefix, webViewTargetDurationInMillis);
        Assert.AreEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }
}
