using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableHue : Themeable
{
    [Delayed]
    public string colorName;
    public ImageHueHelper target;

#if UNITY_EDITOR
    private string lastColorName;

    override protected void Start()
    {
        base.Start();
        lastColorName = colorName;
    }

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

        ImageHueHelper targetImage = target != null ? target : GetComponent<ImageHueHelper>();
        if (targetImage == null)
        {
            Debug.LogWarning($"Target is null and GameObject does not have an ImageHueHelper Component", gameObject);
            return;
        }

        if (theme.TryFindColor(colorName, out Color32 loadedColor))
        {
            targetImage.SetHueByColor(loadedColor);
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
