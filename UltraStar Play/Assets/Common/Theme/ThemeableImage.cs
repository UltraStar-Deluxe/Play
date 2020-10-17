using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableImage : Themeable
{
    [Delayed]
    public string imagePath;
    public Image target;

#if UNITY_EDITOR
    private string lastImagePath;

    override protected void Start()
    {
        base.Start();
        lastImagePath = imagePath;
    }

    override protected void Update()
    {
        base.Update();

        if (imagePath != lastImagePath)
        {
            lastImagePath = imagePath;
            ReloadResources(ThemeManager.CurrentTheme);
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
            Debug.LogWarning($"Missing image file path", gameObject);
            return;
        }

        Image targetImage = target != null ? target : GetComponent<Image>();
        if (targetImage == null)
        {
            Debug.LogWarning($"Target is null and GameObject does not have an Image Component", gameObject);
            return;
        }

        theme.LoadSprite(imagePath,
            (loadedSprite) => targetImage.sprite = loadedSprite);
    }
}
