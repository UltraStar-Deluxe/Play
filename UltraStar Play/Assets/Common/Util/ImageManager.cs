
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImageManager
{

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static Sprite LoadSprite(string path)
    {
        if (!spriteCache.TryGetValue(path, out Sprite sprite))
        {
            int width = 256;
            int height = 256;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(bytes);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1.0f);
            if (sprite != null)
            {
                spriteCache.Add(path, sprite);
            }
        }
        return sprite;
    }
}