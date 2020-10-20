using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ImageHueHelper))]
[ExecuteInEditMode]
public class ThemeableHue : Themeable
{
    [Delayed]
    public string colorName;

    private ImageHueHelper target;

    private void Awake()
    {
        target = GetComponent<ImageHueHelper>();
    }

#if UNITY_EDITOR
    private string lastColorName;

    override protected void Start()
    {
        target = GetComponent<ImageHueHelper>();
        base.Start();
        lastColorName = colorName;
    }

    private void Update()
    {
        if (lastColorName != colorName)
        {
            lastColorName = colorName;
            ReloadResources(ThemeManager.CurrentTheme);
        }
    }

    override public List<UnityEngine.Object> GetAffectedObjects()
    {
        return new List<UnityEngine.Object> { target, target.GetComponent<Image>() };
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
            target.SetHueByColor(loadedColor);
        }
        else
        {
            Debug.LogError($"Could not load color {colorName}", gameObject);
        }
    }
}
