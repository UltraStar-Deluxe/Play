using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

/**
 * Tests that the audio support provider starts at the correct position when using #START tag in UltraStar song file.
 */
public class AudioSupportProviderStartPositionTest : AbstractMediaFileFormatTest
{
    private const string TxtFilePath = "ogg-WithStartTag.txt";
    private const double ExpectedStartPositionInMillis = 2000;

    protected override void ConfigureTestSettings(TestSettings settings)
    {
        base.ConfigureTestSettings(settings);
        settings.AcceptedWebViewHosts = new List<string> { "youtube.com" };
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaUnity()
    {
        ConfigureMediaApiSettings(EApiUsage.Enabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(TxtFilePath, typeof(AudioSourceAudioSupportProvider));
        yield return AssertSongAudioPlayerPosition(ExpectedStartPositionInMillis);
    }

    [UnityTest]
    public IEnumerator ShouldLoadViaAvpro()
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Enabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(TxtFilePath, typeof(AvproAudioSupportProvider));
        yield return AssertSongAudioPlayerPosition(ExpectedStartPositionInMillis);
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaVlc()
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Disabled, EApiUsage.Enabled);
        yield return SongAudioPlayerShouldLoadFileAsync(TxtFilePath, typeof(VlcAudioSupportProvider));
        yield return AssertSongAudioPlayerPosition(ExpectedStartPositionInMillis);
    }
    
    [UnityTest]
    public IEnumerator ShouldLoadViaWebView()
    {
        ConfigureMediaApiSettings(EApiUsage.Disabled, EApiUsage.Disabled, EApiUsage.Disabled);
        yield return SongAudioPlayerShouldLoadFileAsync(
            "WebViewTests/AudioUrlOnly-WithStartTag.txt",
            typeof(WebViewAudioSupportProvider),
            19000,
            30000);
        yield return AssertSongAudioPlayerPosition(8000);
    }

    private static void ConfigureMediaApiSettings(EApiUsage unity, EApiUsage avpro, EApiUsage vlc)
    {
        SettingsManager.Instance.Settings.UnityMediaApiUsage = unity;
        SettingsManager.Instance.Settings.AvProApiUsage = avpro;
        SettingsManager.Instance.Settings.VlcApiUsage = vlc;
    }
    
    private IEnumerator AssertSongAudioPlayerPosition(double expectedPositionInMillis)
    {
        yield return new WaitForSeconds(0.1f);
        double toleranceFactor = 0.9; // Add tolerance because of inaccurate VLC time.
        Assert.That(songAudioPlayer.PositionInMillis, Is.GreaterThanOrEqualTo(expectedPositionInMillis * toleranceFactor));
    }
}
