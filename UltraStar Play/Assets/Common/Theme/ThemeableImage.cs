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

#if UNITY_EDITOR
    private EImageResource lastImageResource = EImageResource.NONE;
#endif

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
            Theme currentTheme = ThemeManager.Instance.GetCurrentTheme();
            ReloadResources(currentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
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

        Sprite loadedSprite = LoadResourceFromTheme<Sprite>(theme, imagePath);
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
