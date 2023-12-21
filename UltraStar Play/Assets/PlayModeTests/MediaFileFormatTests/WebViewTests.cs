using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WebViewTests : AbstractMediaFileFormatTests
{
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
