using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
    public string colorName;
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
        if (string.IsNullOrEmpty(colorName))
        {
            Debug.LogWarning($"Missing image name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        if (TryLoadColorFromTheme(colorName, out Color loadedColor))
        {
            target.color = loadedColor;
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
