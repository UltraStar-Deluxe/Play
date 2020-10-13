using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
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
            colorName = colorResource.GetName();
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
        if (string.IsNullOrEmpty(colorName))
        {
            Debug.LogWarning($"Missing color name", gameObject);
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"Target is null", gameObject);
            return;
        }

        if (theme.TryFindColor(colorName, out Color32 loadedColor))
        {
            target.color = loadedColor;
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
