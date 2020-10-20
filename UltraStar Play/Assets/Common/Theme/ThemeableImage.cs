using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class ThemeableImage : Themeable
{
    [Delayed]
    public string imagePath;

    private Image target;

    private void Awake()
    {
        target = GetComponent<Image>();
    }

#if UNITY_EDITOR
    private string lastImagePath;

    override protected void Start()
    {
        target = GetComponent<Image>();
        base.Start();
        lastImagePath = imagePath;
    }

    private void Update()
    {
        if (imagePath != lastImagePath)
        {
            lastImagePath = imagePath;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }

    override public List<UnityEngine.Object> GetAffectedObjects()
    {
        return new List<UnityEngine.Object> { target };
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
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        Sprite newSprite = theme.LoadSprite(imagePath);
        if (newSprite == null)
        {
            Debug.LogWarning($"Could not load file '{imagePath}' from theme '{theme.Name}'");
        }
        else
        {
            target.sprite = newSprite;
        }
    }
}
