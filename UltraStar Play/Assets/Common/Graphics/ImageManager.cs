
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// Handles loading and caching of images.
public class ImageManager
{
    // When the cache has reached the critical size, then unused sprites are searched in the scene
    // and removed from memory.
    private static readonly int criticalCacheSize = 50;
    private static readonly Dictionary<string, CachedSprite> spriteCache = new Dictionary<string, CachedSprite>();

    public static Sprite LoadSprite(string path)
    {
        if (!spriteCache.TryGetValue(path, out CachedSprite cachedSprite))
        {
            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist: " + path);
                return null;
            }

            Sprite sprite = CreateNewSprite(path);
            if (sprite == null)
            {
                Debug.LogError("Could not create sprite from path: " + path);
                return null;
            }

            // Check critical size of cache BEFORE adding the new sprite.
            // (Otherwise the new sprite will be removed immediately because it is not used yet.)
            if (spriteCache.Count >= criticalCacheSize)
            {
                RemoveUnusedSpritesFromCache();
            }

            // Cache the new sprite.
            cachedSprite = new CachedSprite(path, sprite);
            spriteCache.Add(path, cachedSprite);
            return sprite;
        }
        return cachedSprite.Sprite;
    }

    private static Sprite CreateNewSprite(string path)
    {
        int width = 256;
        int height = 256;
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1.0f);
        return sprite;
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
        spriteCache.Remove(cachedSprite.Path);
        // Destoying the texture is important to free the memory.
        GameObject.Destroy(cachedSprite.Sprite.texture);
        GameObject.Destroy(cachedSprite.Sprite);
        // Trigger garbage collection of used resources.
        Resources.UnloadUnusedAssets();
    }

    private class CachedSprite
    {
        public string Path { get; private set; }
        public Sprite Sprite { get; private set; }

        public CachedSprite(string path, Sprite sprite)
        {
            Path = path;
            Sprite = sprite;
        }
    }
}