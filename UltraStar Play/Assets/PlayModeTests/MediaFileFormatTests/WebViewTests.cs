using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WebViewTests : AbstractMediaFileFormatTests
{
    protected override void ConfigureTestSettings(TestSettings settings)
    {
        settings.AcceptedWebViewHosts = new List<string>()
        {
            "youtube.com",
        };
    }

    [UnityTest]
    public IEnumerator WebViewOnlyTest()
    {
        yield return WebViewFileTest("WebViewOnly-", webViewTargetDurationInMillis);
        Assert.AreEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator WebViewAndMissingAudioTest()
    {
        yield return WebViewFileTest("WebViewAndMissingAudio-", webViewTargetDurationInMillis);
        Assert.AreEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator WebViewAsAudioTest()
    {
        yield return WebViewFileTest("WebViewAsAudio-", webViewTargetDurationInMillis);
        Assert.AreEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }

    [UnityTest]
    public IEnumerator WebViewAndExistingAudioTest()
    {
        yield return WebViewFileTest("WebViewAndExistingAudio-", localFileTargetDurationInMillis);
        Assert.AreNotEqual(EAudioSupportProvider.WebView, SongAudioPlayer.AudioSupportProvider);
    }
}
