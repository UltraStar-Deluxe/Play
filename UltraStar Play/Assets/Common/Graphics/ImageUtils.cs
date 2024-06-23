using System;
using UnityEngine;
using UnityEngine.Networking;

public static class ImageUtils
{
    public static Sprite CreateUncachedSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect);
    }

    public static UnityWebRequest CreateTextureRequest(Uri uriHandle)
    {
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uriHandle);
        return webRequest;
    }
}
