using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

/**
 * Handles loading and caching of images.
 */
public class ImageManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ImageManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ImageManager>();

    private readonly HashSet<ISpriteHolder> spriteHolders = new();

    /**
     * When the cache has reached this critical size
     * then unused sprites are searched in the scene and removed from memory.
     */
    private readonly int criticalCacheSize = 50;
    private readonly Dictionary<string, CachedSprite> spriteCache = new();
    private readonly Dictionary<string, RunningRequest> runningRequests = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void OnDestroySingleton()
    {
        ClearCache();
    }

    public static void AddSpriteHolder(ISpriteHolder spriteHolder)
    {
        Instance.DoAddSpriteHolder(spriteHolder);
    }

    private void DoAddSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Add(spriteHolder);
    }

    public void RemoveSpriteHolder(ISpriteHolder spriteHolder)
    {
        Instance.DoRemoveSpriteHolder(spriteHolder);
    }

    private void DoRemoveSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Remove(spriteHolder);
    }

    public static void ReloadImage(string uri, UIDocument uiDocument)
    {
        Instance.ReloadImageAsync(uri, uiDocument);
    }

    private async void ReloadImageAsync(string uri, UIDocument uiDocument)
    {
        if (!spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            || cachedSprite.Sprite == null)
        {
            // Nothing using this right now. No need to reload anything.
        }

        // Find VisualElements that use this sprite. Update them with a new sprite.
        List<VisualElement> visualElementsUsingTheSprite = uiDocument.rootVisualElement.Query<VisualElement>()
            .Where(visualElement => visualElement != null
                                    && visualElement.style.backgroundImage != null
                                    && visualElement.style.backgroundImage == new StyleBackground(cachedSprite.Sprite))
            .ToList();

        // Remove from cache before reloading.
        Log.Debug(() => $"Remove sprite from cache before reload. uri: '{uri}'");
        RemoveCachedSprite(cachedSprite);

        Sprite sprite = await LoadSpriteFromUriAsync(uri);
        visualElementsUsingTheSprite.ForEach(it => it.style.backgroundImage = new StyleBackground(sprite));
    }

    public static async Awaitable<Sprite> LoadSpriteFromUriAsync(string uri)
    {
        return await Instance.DoLoadSpriteFromUriAsync(uri);
    }

    private async Awaitable<Sprite> DoLoadSpriteFromUriAsync(string uri)
    {
        if (uri.IsNullOrEmpty())
        {
            throw new NullReferenceException("Failed to load Sprite, URI is null or empty");
        }

        if (spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            && cachedSprite?.Sprite != null)
        {
            Log.Verbose(() => $"Reusing cached sprite: uri '{uri}'");
            return cachedSprite.Sprite;
        }
        
        // Multiple requests should load the image only once.
        // And when eventually loaded, then all callers should receive the same instance.
        if (runningRequests.TryGetValue(uri, out RunningRequest runningRequest))
        {
            Log.Verbose(() => $"Awaiting already running request for sprite: uri '{uri}'");
            return await WaitForRunningRequest(runningRequest);
        }
        runningRequest = new();
        runningRequests[uri] = runningRequest;
        try
        {
            Sprite sprite = await DoLoadUncachedSpriteFromUriAsync(uri);
            runningRequest.result = sprite;
            return sprite;
        }
        catch (Exception e)
        {
            runningRequest.exception = e;
            throw;
        }
        finally
        {
            runningRequests.Remove(uri);
        }
    }

    private async Awaitable<Sprite> DoLoadUncachedSpriteFromUriAsync(string uri)
    {
        Texture2D loadedTexture;
        try
        {
            using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(new Uri(uri));
            await WebRequestUtils.SendWebRequestAsync(webRequest);

            loadedTexture = DownloadHandlerTexture.GetContent(webRequest);
        }
        catch (Exception ex)
        {
            throw new LoadImageException($"Failed to load Texture2D from URI: '{uri}'", ex);
        }

        Sprite sprite = CreateUncachedSprite(loadedTexture);
        AddSpriteToCache(sprite, uri);
        Log.Verbose(() => $"Added sprite to cache: cache size {spriteCache.Count}, uri '{uri}'");
        return sprite;
    }
    
    private void AddSpriteToCache(Sprite sprite, string source)
    {
        // Check critical size of cache BEFORE adding the new sprite.
        // (Otherwise the new sprite will be removed immediately because it is not used yet.)
        if (spriteCache.Count >= criticalCacheSize)
        {
            DoRemoveUnusedSpritesFromCache();
        }

        // Cache the new sprite.
        CachedSprite cachedSprite = new(source, sprite);

        if (spriteCache.ContainsKey(source))
        {
            Debug.LogWarning($"Sprite was already cached: source '{source}'");
        }
        spriteCache[source] = cachedSprite;
    }

    public void ClearCache()
    {
        Log.Verbose(() => $"Clearing sprite cache. current count: {spriteCache.Count}");
        foreach (CachedSprite cachedSprite in new List<CachedSprite>(spriteCache.Values))
        {
            RemoveCachedSprite(cachedSprite);
        }
        spriteCache.Clear();
    }

    public static void RemoveUnusedSpritesFromCache()
    {
        if (Instance == null)
        {
            return;
        }
        Instance.DoRemoveUnusedSpritesFromCache();
    }

    private void DoRemoveUnusedSpritesFromCache()
    {
        using IDisposable d = new DisposableStopwatch("RemoveUnusedSpritesFromCache");
        
        int countBefore = spriteCache.Count;
        HashSet<Sprite> usedSprites = new();
        // Remember the sprites of all registered ISpriteHolder as still in use.
        spriteHolders.ForEach(spriteHolder => usedSprites.AddRange(spriteHolder.GetSprites()));

        // Iterate over all sprites in VisualElements in the scene and remember them as still in use.
        UIDocument uiDocument = UIDocumentUtils.FindUIDocumentOrThrow();
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement
                .Query()
                .ForEach(visualElement =>
                {
                    if (visualElement.style.backgroundImage != null
                        && visualElement.style.backgroundImage.value != null
                        && visualElement.style.backgroundImage.value.sprite != null)
                    {
                        usedSprites.Add(visualElement.style.backgroundImage.value.sprite);
                    }
                });
        }

        // Remove sprites from the cache that have not been marked as still in use.
        List<CachedSprite> unusedSprites = spriteCache.Values
            .Where(cachedSprite => !usedSprites.Contains(cachedSprite.Sprite))
            .ToList();

        unusedSprites.ForEach(RemoveCachedSprite);
        Log.Debug(() => $"Removed unused sprites from cache. count before: {countBefore}, count after: {spriteCache.Count}, removed {unusedSprites.Count}");
    }

    private void RemoveCachedSprite(CachedSprite cachedSprite)
    {
        spriteCache.Remove(cachedSprite.Source);
        // Destroying the texture is important to free the memory.
        if (cachedSprite.Sprite != null)
        {
            if (cachedSprite.Sprite.texture != null)
            {
                GameObjectUtils.Destroy(cachedSprite.Sprite.texture);
            }
            GameObjectUtils.Destroy(cachedSprite.Sprite);
        }
    }

    private static Sprite CreateUncachedSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect);
    }

    private class CachedSprite
    {
        public string Source { get; private set; }
        public Sprite Sprite { get; private set; }

        public CachedSprite(string source, Sprite sprite)
        {
            Source = source;
            Sprite = sprite;
        }
    }

    private async Awaitable<Sprite> WaitForRunningRequest(RunningRequest runningRequest)
    {
        while (runningRequest.result == null
               && runningRequest.exception == null)
        {
            await Awaitable.EndOfFrameAsync();
        }
        return runningRequest.result ?? throw runningRequest.exception;
    }

    // Custom solution to resolve multiple awaits because AwaitableCompletionSource did not work as expected.
    private class RunningRequest
    {
        public string uri;
        public Sprite result;
        public Exception exception;
    }
}
