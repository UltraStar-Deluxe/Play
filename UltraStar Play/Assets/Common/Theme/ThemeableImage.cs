using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableImage : Themeable
{
    public string imageName;
    public Image target;

    void OnEnable()
    {
        if (target == null)
        {
            target = GetComponent<Image>();
        }
    }

    public override void ReloadResources()
    {
        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogWarning($"Missing image name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        Sprite loadedSprite = LoadAssetFromTheme<Sprite>(imageName);
        if (loadedSprite == null)
        {
            Debug.LogError($"Could not load image {imageName}", gameObject);
        }
        else
        {
            target.sprite = loadedSprite;
        }
    }
}
