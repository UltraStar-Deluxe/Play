using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableImage : Themeable
{
    [ReadOnly]
    public string imagePath;
    public EImageResource imageResource = EImageResource.NONE;
    public Image target;

    private EImageResource lastImageResource = EImageResource.NONE;

    void OnEnable()
    {
        if (target == null)
        {
            target = GetComponent<Image>();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (imageResource != EImageResource.NONE && lastImageResource != imageResource)
        {
            lastImageResource = imageResource;
            imagePath = imageResource.GetPath();
            ReloadResources();
        }
    }
#endif

    public override void ReloadResources()
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            Debug.LogWarning($"Theme resource not specified", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        Sprite loadedSprite = LoadResourceFromTheme<Sprite>(imagePath);
        if (loadedSprite == null)
        {
            Debug.LogError($"Could not load image {imagePath}", gameObject);
        }
        else
        {
            target.sprite = loadedSprite;
        }
    }
}
