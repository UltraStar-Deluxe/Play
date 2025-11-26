using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioManagerTest : AbstractPlayModeTest
{
    private static readonly string folderPath = Application.dataPath + "/PlayModeTests/AudioManagerTest";
    private static readonly string audioPath = folderPath + "/HoliznaCC0 - To Be an Animal - Excerpt.ogg";

    [SetUp]
    public void SetUp()
    {
        AudioManager.Instance.ClearCache();
    }

    [UnityTest]
    public IEnumerator ConcurrentCallsShouldReturnSameAudioInstance() => ConcurrentCallsShouldReturnSameAudioInstanceAsync();
    private async Awaitable ConcurrentCallsShouldReturnSameAudioInstanceAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        
        List<AudioClip> loadedAudioClips = new List<AudioClip>();

        // Can be better tested with an artificial delay or breakpoint in AudioManager.
        // All these requests are fired on the main thread, so a single image likely loads to fast otherwise.
        int requestCount = 10;
        for (int i = 0; i < requestCount; i++)
        {
            int i1 = i; // Copy to effectively final local variable for closure.
            ThreadUtils.RunOnMainThread(async () =>
            {
                Debug.Log("Loading AudioClip " + i1);
                AudioClip loadedAudioClip = await AudioManager.LoadAudioClipFromUriAsync(audioPath, false);
                Debug.Log("Loaded AudioClip " + i1);
                loadedAudioClips.Add(loadedAudioClip);
            });
        }

        await ConditionUtils.WaitForConditionAsync(() => loadedAudioClips.Count == requestCount,
            new WaitForConditionConfig {description = "All sprites loaded", timeoutInMillis = 10_000});
        Assert.AreEqual(requestCount, loadedAudioClips.Count);
        AudioClip firstAudioClip = loadedAudioClips.First();
        loadedAudioClips.ForEach(audioClip => Assert.IsTrue(ReferenceEquals(firstAudioClip, audioClip)));
    }
}
