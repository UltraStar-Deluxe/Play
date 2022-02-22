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

    public static Sprite LoadSpriteFromFile(string path)
    {
        return LoadSprite("file://" + path);
    }

    public static Sprite LoadSprite(string path)
    {
        if (spriteCache.TryGetValue(path, out CachedSprite cachedSprite)
            && cachedSprite.Sprite != null)
        {
            return cachedSprite.Sprite;
        }

        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return null;
        }

        Sprite sprite = CreateNewSpriteUncached(path);
        if (sprite == null)
        {
            Debug.LogError("Could not create sprite from path: " + path);
            return null;
        }

        AddSpriteToCache(sprite, path);
        return sprite;
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

        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }
        coroutineManager.StartCoroutineAlsoForEditor(WebRequestUtils.LoadTexture2DFromUri(uri, DoCacheSpriteThenOnSuccess, onFailure));
    }

    public static Texture2D LoadTextureUncached(string path)
    {
        int width = 256;
        int height = 256;
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        return texture;
    }

    private static Sprite CreateNewSpriteUncached(string path)
    {
        Texture2D texture = LoadTextureUncached(path);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1.0f);
        return sprite;
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
