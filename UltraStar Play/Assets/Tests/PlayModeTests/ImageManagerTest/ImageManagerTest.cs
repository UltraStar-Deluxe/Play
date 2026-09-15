using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ImageManagerTest : AbstractPlayModeTest
{
    private static readonly string folderPath = $"{Application.dataPath}/Tests/PlayModeTests/ImageManagerTest";
    private static readonly string imagePath = folderPath + "/LogoSmall-8k.png";

    [SetUp]
    public void SetUp()
    {
        ImageManager.Instance.ClearCache();
    }

    [UnityTest]
    public IEnumerator ConcurrentCallsShouldReturnSameImageInstance() => ConcurrentCallsShouldReturnSameImageInstanceAsync();
    private async Awaitable ConcurrentCallsShouldReturnSameImageInstanceAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        
        List<Sprite> loadedSprites = new List<Sprite>();

        // Can be better tested with an artificial delay or breakpoint in ImageManager.
        // All these requests are fired on the main thread, so a single image likely loads to fast otherwise.
        int requestCount = 10;
        for (int i = 0; i < requestCount; i++)
        {
            int i1 = i; // Copy to effectively final local variable for closure.
            ThreadUtils.RunOnMainThread(async () =>
            {
                Debug.Log("Loading sprite " + i1);
                Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(imagePath);
                Debug.Log("Loaded sprite " + i1);
                loadedSprites.Add(loadedSprite);
            });
        }

        await ConditionUtils.WaitForConditionAsync(() => loadedSprites.Count == requestCount,
            new WaitForConditionConfig {description = "All sprites loaded", timeoutInMillis = 10_000});
        Assert.AreEqual(requestCount, loadedSprites.Count);
        Sprite firstSprite = loadedSprites.First();
        loadedSprites.ForEach(sprite => Assert.IsTrue(ReferenceEquals(firstSprite, sprite)));
    }
}
