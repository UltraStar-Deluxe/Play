using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
    [Delayed]
    public string colorName;
    public Image target;

#if UNITY_EDITOR
    private string lastColorName;

    override protected void Update()
    {
        base.Update();

        if (lastColorName != colorName)
        {
            lastColorName = colorName;
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
        if (string.IsNullOrEmpty(colorName))
        {
            Debug.LogWarning($"Missing color name", gameObject);
            return;
        }

        Image targetImage = target != null ? target : GetComponent<Image>();
        if (targetImage == null)
        {
            Debug.LogWarning($"Target is null and GameObject does not have an Image Component", gameObject);
            return;
        }

        if (theme.TryFindColor(colorName, out Color32 loadedColor))
        {
            targetImage.color = loadedColor;
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
