using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteInEditMode]
public class ThemeableColor : Themeable
{
    [Delayed]
    public string colorName;

    private Image target;

    private void Awake()
    {
        target = GetComponent<Image>();
    }

#if UNITY_EDITOR
    private string lastColorName;

    override protected void Start()
    {
        target = GetComponent<Image>();
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
