using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

// Handles loading and caching of images.
public static class ImageManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        spriteHolders.Clear();
        ClearCache();
    }

    private static readonly HashSet<ISpriteHolder> spriteHolders = new();

    // When the cache has reached the critical size, then unused sprites are searched in the scene
    // and removed from memory.
    private static readonly int criticalCacheSize = 50;
    private static readonly Dictionary<string, CachedSprite> spriteCache = new();

    public static void AddSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Add(spriteHolder);
    }

    public static void RemoveSpriteHolder(ISpriteHolder spriteHolder)
    {
        spriteHolders.Remove(spriteHolder);
    }

    public static void LoadSpriteFromFile(string path, Action<Sprite> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Image file does not exist: " + path);
            return;
        }

        LoadSpriteFromUri(path, onSuccess, onFailure);
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
            .Where(visualElement => visualElement.style.backgroundImage == new StyleBackground(cachedSprite.Sprite))
            .ToList();

        // Remove from cache before reloading.
        RemoveCachedSprite(cachedSprite);

        LoadSpriteFromUri(uri, sprite => visualElementsUsingTheSprite
            .ForEach(it => it.style.backgroundImage = new StyleBackground(sprite)));
    }

    public static void LoadSpriteFromUri(string uri, Action<Sprite> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        if (spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            && cachedSprite.Sprite != null)
        {
            onSuccess(cachedSprite.Sprite);
            return;
        }

        void DoCacheSpriteThenOnSuccess(Texture2D loadedTexture)
        {
            Sprite sprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f));
            AddSpriteToCache(sprite, uri);
            onSuccess(sprite);
        }

        UiManager.Instance.StartCoroutine(WebRequestUtils.LoadTexture2DFromUri(uri, DoCacheSpriteThenOnSuccess, onFailure));
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

    public static void ClearCache()
    {
        foreach (CachedSprite cachedSprite in new List<CachedSprite>(spriteCache.Values))
        {
            RemoveCachedSprite(cachedSprite);
        }
        spriteCache.Clear();
    }

    private static void RemoveUnusedSpritesFromCache()
    {
        HashSet<Sprite> usedSprites = new();
        // Remember the sprites of all registered ISpriteHolder as still in use.
        spriteHolders.ForEach(spriteHolder => usedSprites.AddRange(spriteHolder.GetSprites()));

        // Iterate over all sprites in VisualElements in the scene and remember them as still in use.
        UIDocument uiDocument = GameObject.FindObjectOfType<UIDocument>();
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
        foreach (CachedSprite cachedSprite in new List<CachedSprite>(spriteCache.Values))
        {
            if (!usedSprites.Contains(cachedSprite.Sprite))
            {
                RemoveCachedSprite(cachedSprite);
            }
        }
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
