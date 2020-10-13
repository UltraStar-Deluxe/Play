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
        if (imageResource != EImageResource.NONE
            && imagePath != imageResource.GetPath())
        {
            imagePath = imageResource.GetPath();
            ReloadResources(ThemeManager.Instance.CurrentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
    {
        if (theme == null)
        {
            Debug.LogError("Theme is null", gameObject);
            return;
        }
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

        Sprite loadedSprite = theme.FindResource<Sprite>(imagePath);
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
