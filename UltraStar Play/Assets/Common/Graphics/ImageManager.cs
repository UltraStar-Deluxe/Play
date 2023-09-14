using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

// Handles loading and caching of images.
public class ImageManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        spriteHolders.Clear();
        ClearCache();
    }

    public static ImageManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ImageManager>();

    private static readonly HashSet<ISpriteHolder> spriteHolders = new();

    // When the cache has reached the critical size, then unused sprites are searched in the scene
    // and removed from memory.
    private static readonly int criticalCacheSize = 50;
    private static readonly Dictionary<string, CachedSprite> spriteCache = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    public static void AddSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Add(spriteHolder);
    }

    public static void RemoveSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Remove(spriteHolder);
    }

    public static IObservable<Sprite> LoadSpriteFromUri(string path)
    {
        return Observable.Create<Sprite>(o =>
        {
            DoLoadSpriteFromUri(path,
                loadedSprite =>
                {
                    o.OnNext(loadedSprite);
                    o.OnCompleted();
                },
                () => o.OnError(new LoadImageException($"Failed to load sprite: {path}")));
            return Disposable.Empty;
        });
    }

    public static void ReloadImage(string uri, UIDocument uiDocument)
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

        DoLoadSpriteFromUri(uri, sprite => visualElementsUsingTheSprite
            .ForEach(it => it.style.backgroundImage = new StyleBackground(sprite)));
    }

    private static void DoLoadSpriteFromUri(string uri, Action<Sprite> onSuccess, Action onFailure = null)
    {
        if (spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            && cachedSprite?.Sprite != null)
        {
            onSuccess?.Invoke(cachedSprite.Sprite);
            return;
        }

        void DoCacheSpriteThenOnSuccess(Texture2D loadedTexture)
        {
            if (loadedTexture == null)
            {
                Debug.LogError($"Loaded texture is null for URI {uri}");
                onFailure?.Invoke();
                return;
            }
            Sprite sprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u,  SpriteMeshType.FullRect);
            AddSpriteToCache(sprite, uri);
            onSuccess?.Invoke(sprite);
        }

        void OnFailureOfUnityWebRequest(UnityWebRequest request)
        {
            onFailure?.Invoke();
        }

        Instance.StartCoroutine(WebRequestUtils.LoadTexture2DFromUri(uri, DoCacheSpriteThenOnSuccess, OnFailureOfUnityWebRequest));
    }

    private static void AddSpriteToCache(Sprite sprite, string source)
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

    private static void ClearCache()
    {
        foreach (CachedSprite cachedSprite in new List<CachedSprite>(spriteCache.Values))
        {
            RemoveCachedSprite(cachedSprite);
        }
        spriteCache.Clear();
    }

    public static void RemoveUnusedSpritesFromCache()
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

    private static void RemoveCachedSprite(CachedSprite cachedSprite)
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
