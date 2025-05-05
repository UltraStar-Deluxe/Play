using System.Collections;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;

public class VocalsAndInstrumentalAudioTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SingScene.ToString();

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongAudioPlayer songAudioPlayer;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SingSceneAlternativeAudioPlayer singSceneAlternativeAudioPlayer;

    [Inject]
    private Settings settings;

    [UnityTest]
    [Ignore("AudioSource.time is seem returned as 0 although the synchronization seems to work")] // TODO: Fix this test
    public IEnumerator ShouldBeInSyncWithSongAudioPlayer()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given
        Assert.IsTrue(settings.GetType() == typeof(TestSettings));
        Assert.AreEqual(100, settings.VocalsAudioVolumePercent);

        yield return new WaitForSeconds(1.5f);

        // When
        settings.VocalsAudioVolumePercent = 50;
        yield return new WaitForSeconds(1f);

        // Then
        AssertTimesAreEqual();
        yield return new WaitForSeconds(0.5f);

        AssertTimesAreEqual();
        yield return null;
    }

    private void AssertTimesAreEqual()
    {
        Assert.AreEqual(songAudioPlayer.PositionInSeconds, singSceneAlternativeAudioPlayer.vocalsAudioSource.time, 0.1f);
        Assert.AreEqual(songAudioPlayer.PositionInSeconds, singSceneAlternativeAudioPlayer.instrumentalAudioSource.time, 0.1f);
    }
}
