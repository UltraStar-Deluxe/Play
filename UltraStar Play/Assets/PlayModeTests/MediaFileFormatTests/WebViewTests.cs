using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WebViewTests : AbstractMediaFileFormatTests
{
    private static string[] shouldUseLocalAudioFilePrefixes = new string[]
    {
        "AudioUrlAndExistingAudio-",
    };

    private static string[] shouldUseWebViewFilePrefixes = new string[]
    {
        "AudioUrlAndMissingAudio-",
        "AudioUrlOnly-",
        "AudioOnly-",
        "VideoUrlOnly-",
        "WebsiteOnly-",
    };

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        settings.AcceptedWebViewHosts = new List<string>()
        {
            "youtube.com",
        };
    }

    [UnityTest]
    public IEnumerator ShouldUseLocalAudioTest([ValueSource(nameof(shouldUseLocalAudioFilePrefixes))] string filePrefix)
    {
        yield return WebViewFileTest(filePrefix, localFileTargetDurationInMillis);
        Assert.AreNotEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator ShouldUseWebView([ValueSource(nameof(shouldUseWebViewFilePrefixes))] string filePrefix)
    {
        yield return WebViewFileTest(filePrefix, webViewTargetDurationInMillis);
        Assert.AreEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }
}
