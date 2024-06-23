using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

/**
 * Handles loading and caching of images.
 */
public class ImageManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ImageManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ImageManager>();

    private readonly HashSet<ISpriteHolder> spriteHolders = new();

    // When the cache has reached the critical size, then unused sprites are searched in the scene
    // and removed from memory.
    private readonly int criticalCacheSize = 50;
    private readonly Dictionary<string, CachedSprite> spriteCache = new();

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
        Instance.DoReloadImage(uri, uiDocument);
    }

    private void DoReloadImage(string uri, UIDocument uiDocument)
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
        RemoveCachedSprite(cachedSprite);

        LoadSpriteFromUri(uri)
            .Subscribe(sprite =>
            {
                visualElementsUsingTheSprite.ForEach(it => it.style.backgroundImage = new StyleBackground(sprite));
            });
    }

    public static Sprite LoadSpriteFromUriImmediately(string uri)
    {
        Sprite result = null;
        // Load with busy waiting
        Instance.DoLoadSpriteFromUri(uri, true)
            .Subscribe(sprite => result = sprite);
        return result;
    }

    public static IObservable<Sprite> LoadSpriteFromUri(string uri)
    {
        return Instance.DoLoadSpriteFromUri(uri, false);
    }

    private IObservable<Sprite> DoLoadSpriteFromUri(string uri, bool busyWaiting)
    {
        if (uri.IsNullOrEmpty())
        {
            return ObservableUtils.LogExceptionThenThrow<Sprite>(new NullReferenceException("Cannot load Sprite, URI is null or empty"));
        }

        if (spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            && cachedSprite?.Sprite != null)
        {
            return Observable.Return<Sprite>(cachedSprite.Sprite);
        }

        return Observable.Create<Sprite>(o =>
        {
            CancellationTokenSource cancellationTokenSource = new();

            // Send web request
            UnityWebRequest webRequest = ImageUtils.CreateTextureRequest(new Uri(uri));
            webRequest.SendWebRequest();

            // Check web request result in coroutine
            Instance.StartCoroutine(CoroutineUtils.WebRequestCoroutine(webRequest,
                downloadHandler =>
                {
                    if (webRequest.downloadHandler is DownloadHandlerTexture downloadHandlerTexture
                        && downloadHandlerTexture.texture != null)
                    {
                        Texture2D loadedTexture = downloadHandlerTexture.texture;
                        Sprite sprite = ImageUtils.CreateUncachedSprite(loadedTexture);
                        AddSpriteToCache(sprite, uri);

                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            o.OnNext(sprite);
                        }
                        o.OnCompleted();
                    }
                    else if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        o.OnError(new LoadImageException($"Failed to load Texture2D from URI: '{uri}'."));
                    }
                },
                ex =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to load Texture2D from URI: '{uri}': {ex.Message}");
                    o.OnError(ex);
                },
                busyWaiting));
            return Disposable.Create(() => cancellationTokenSource.Cancel());
        });
    }

    private void AddSpriteToCache(Sprite sprite, string source)
    {
        // Check critical size of cache BEFORE adding the new sprite.
        // (Otherwise the new sprite will be removed immediately because it is not used yet.)
        if (spriteCache.Count >= criticalCacheSize)
        {
            RemoveUnusedSpritesFromCache();
        }

        // Cache the new sprite.
        CachedSprite cachedSprite = new(source, sprite);
        spriteCache[source] = cachedSprite;
    }

    private void ClearCache()
    {
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
        HashSet<Sprite> usedSprites = new();
        // Remember the sprites of all registered ISpriteHolder as still in use.
        spriteHolders.ForEach(spriteHolder => usedSprites.AddRange(spriteHolder.GetSprites()));

        // Iterate over all sprites in VisualElements in the scene and remember them as still in use.
        UIDocument uiDocument = UIDocumentUtils.FindUIDocumentOrThrow();
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement
                .Query<VisualElement>()
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

        Debug.Log($"Removing {unusedSprites.Count} unused sprites from cache.");
        unusedSprites.ForEach(RemoveCachedSprite);
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
}
