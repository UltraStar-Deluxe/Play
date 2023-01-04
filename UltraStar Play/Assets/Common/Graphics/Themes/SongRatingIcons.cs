using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SongRatingIcons
{
    public string toneDeaf;
    public string amateur;
    public string wannabe;
    public string hopeful;
    public string risingStar;
    public string leadSinger;
    public string superstar;
    public string ultrastar;

    Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();

    public Sprite GetSpriteForRating(ThemeMeta themeMeta, SongRating.ESongRating songRating)
    {
        switch (songRating)
        {
            case SongRating.ESongRating.ToneDeaf: return GetSprite(themeMeta, toneDeaf);
            case SongRating.ESongRating.Amateur: return GetSprite(themeMeta, amateur);
            case SongRating.ESongRating.Wannabe: return GetSprite(themeMeta, wannabe);
            case SongRating.ESongRating.Hopeful: return GetSprite(themeMeta, hopeful);
            case SongRating.ESongRating.RisingStar: return GetSprite(themeMeta, risingStar);
            case SongRating.ESongRating.LeadSinger: return GetSprite(themeMeta, leadSinger);
            case SongRating.ESongRating.Superstar: return GetSprite(themeMeta, superstar);
            case SongRating.ESongRating.Ultrastar: return GetSprite(themeMeta, ultrastar);
            default:
                throw new ArgumentOutOfRangeException(nameof(songRating), songRating, null);
        }
    }

    private Sprite GetSprite(ThemeMeta themeMeta, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        if (loadedSprites.ContainsKey(filePath))
        {
            return loadedSprites[filePath];
        }

        string fullPath = ThemeMetaUtils.GetAbsoluteFilePath(themeMeta, filePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[THEME] Couldn't load image at path: '{fullPath}'");
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(fullPath);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, true)
        {
            alphaIsTransparency = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        texture.LoadImage(imageBytes, true);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }

    public void DestroyLoadedSprites()
    {
        foreach (KeyValuePair<string,Sprite> loadedSprite in loadedSprites)
        {
            GameObject.Destroy(loadedSprite.Value);
        }
        loadedSprites.Clear();
    }
}
