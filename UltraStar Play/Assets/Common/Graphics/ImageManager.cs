using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

// Handles loading and caching of images.
public static class ImageManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        ClearCache();
    }

    // When the cache has reached the critical size, then unused sprites are searched in the scene
    // and removed from memory.
    private static readonly int criticalCacheSize = 50;
    private static readonly Dictionary<string, CachedSprite> spriteCache = new Dictionary<string, CachedSprite>();

    private static CoroutineManager coroutineManager;

    public static void LoadSpriteFromFile(string path, Action<Sprite> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        LoadSpriteFromUri("file://" + path, onSuccess, onFailure);
    }

    public static void LoadSpriteFromUri(string uri, Action<Sprite> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        if (spriteCache.TryGetValue(uri, out CachedSprite cachedSprite)
            && cachedSprite.Sprite != null)
        {
            onSuccess(cachedSprite.Sprite);
            return;
        }

        if (!WebRequestUtils.ResourceExists(uri))
        {
            Debug.LogError("Image resource does not exist: " + uri);
            return;
        }

        void DoCacheSpriteThenOnSuccess(Texture2D loadedTexture)
        {
            Sprite sprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), new Vector2(0.5f, 0.5f));
            AddSpriteToCache(sprite, uri);
            onSuccess(sprite);
        }

        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }
        coroutineManager.StartCoroutineAlsoForEditor(WebRequestUtils.LoadTexture2DFromUri(uri, DoCacheSpriteThenOnSuccess, onFailure));
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
        CachedSprite cachedSprite = new CachedSprite(source, sprite);
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
        HashSet<Sprite> usedSprites = new HashSet<Sprite>();
        // Iterate over all sprites in the scene that are referenced by an Image or ISpriteHolder
        // and remember them as still in use.
        foreach (Transform transform in GameObject.FindObjectsOfType<Transform>())
        {
            Image image = transform.GetComponent<Image>();
            if (image != null)
            {
                usedSprites.Add(image.sprite);
            }
            ISpriteHolder spriteHolder = transform.GetComponent<ISpriteHolder>();
            if (spriteHolder != null)
            {
                usedSprites.Add(spriteHolder.GetSprite());
            }
        }

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
