using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
    [ReadOnly]
    public string colorsFileName = "colors";
    [ReadOnly]
    public string colorName;
    public EColorResource colorResource = EColorResource.NONE;
    public Image target;

#if UNITY_EDITOR
    private EColorResource lastColorResource = EColorResource.NONE;
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
        if (lastColorResource != colorResource)
        {
            lastColorResource = colorResource;
            colorsFileName = colorResource.GetPath();
            colorName = colorResource.GetName();
            Theme currentTheme = ThemeManager.Instance.GetCurrentTheme();
            ReloadResources(currentTheme);
        }
    }
#endif

    public override void ReloadResources(Theme theme)
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

        if (TryLoadColorFromTheme(theme, colorsFileName, colorName, out Color loadedColor))
        {
            target.color = loadedColor;
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
