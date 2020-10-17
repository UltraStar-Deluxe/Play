using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ThemeableSelectableColors : Themeable
{
    [Delayed]
    public string normalColorName;
    [Delayed]
    public string highlightedColorName;
    [Delayed]
    public string pressedColorName;
    [Delayed]
    public string selectedColorName;
    [Delayed]
    public string disabledColorName;

    public Selectable target;

    public float fadeDuration = 0.1f;

#if UNITY_EDITOR
    private string lastNormalColorName;
    private string lastHighlightedColorName;
    private string lastPressedColorName;
    private string lastSelectedColorName;
    private string lastDisabledColorName;

    override protected void Start()
    {
        base.Start();
        lastNormalColorName = normalColorName;
        lastHighlightedColorName = highlightedColorName;
        lastPressedColorName = pressedColorName;
        lastSelectedColorName = selectedColorName;
        lastDisabledColorName = disabledColorName;

        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => RestoreFadeDuration()));
    }

    override protected void Update()
    {
        base.Update();

        RestoreFadeDuration();

        if (lastNormalColorName != normalColorName
            || lastHighlightedColorName != highlightedColorName
            || lastPressedColorName != pressedColorName
            || lastSelectedColorName != selectedColorName
            || lastDisabledColorName != disabledColorName)
        {
            lastNormalColorName = normalColorName;
            lastHighlightedColorName = highlightedColorName;
            lastPressedColorName = pressedColorName;
            lastSelectedColorName = selectedColorName;
            lastDisabledColorName = disabledColorName;

            ReloadResources(ThemeManager.CurrentTheme);
        }
    }
#else
    override protected void Start()
    {
        base.Start();
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => RestoreFadeDuration()));
    }
#endif

    private void RestoreFadeDuration()
    {
        // The fade duration must be 0 when applying new colors.
        // The original fade duration is restored here.
        Selectable targetSelectable = target != null ? target : GetComponent<Selectable>();
        if (targetSelectable != null)
        {
            ColorBlock colorBlock = targetSelectable.colors;
            colorBlock.fadeDuration = fadeDuration;
            targetSelectable.colors = colorBlock;
        }
    }

    public override void ReloadResources(Theme theme)
    {
        if (theme == null)
        {
            Debug.LogError("Theme is null", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(normalColorName))
        {
            Debug.LogWarning($"Missing normalColorName name", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(highlightedColorName))
        {
            Debug.LogWarning($"Missing highlightedColorName name", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(pressedColorName))
        {
            Debug.LogWarning($"Missing pressedColorName name", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(selectedColorName))
        {
            Debug.LogWarning($"Missing selectedColorName name", gameObject);
            return;
        }
        if (string.IsNullOrEmpty(disabledColorName))
        {
            Debug.LogWarning($"Missing disabledColorName name", gameObject);
            return;
        }

        Selectable targetSelectable = target != null ? target : GetComponent<Selectable>();
        if (targetSelectable == null)
        {
            Debug.LogWarning($"Target is null and GameObject does not have a Selectable Component", gameObject);
            return;
        }

        ApplyThemeColors(theme, targetSelectable);
    }

    private void ApplyThemeColors(Theme theme, Selectable selectable)
    {
        ColorBlock colorBlock = selectable.colors;
        // Fade duration must be 0. Otherwise the Selectable will transition from its current color to the new,
        // which results in a frame with wrong colors.
        colorBlock.fadeDuration = 0;

        if (!theme.TryFindColor(highlightedColorName, out Color32 highlightedColor))
        {
            Debug.LogError("Could not load theme color: " + highlightedColorName);
            return;
        }
        colorBlock.highlightedColor = highlightedColor;

        if (!theme.TryFindColor(disabledColorName, out Color32 disabledColor))
        {
            Debug.LogError("Could not load theme color: " + disabledColorName);
            return;
        }
        colorBlock.disabledColor = disabledColor;

        if (!theme.TryFindColor(normalColorName, out Color32 normalColor))
        {
            Debug.LogError("Could not load theme color: " + normalColorName);
            return;
        }
        colorBlock.normalColor = normalColor;

        if (!theme.TryFindColor(selectedColorName, out Color32 selectedColor))
        {
            Debug.LogError("Could not load theme color: " + selectedColorName);
            return;
        }
        colorBlock.selectedColor = selectedColor;

        if (!theme.TryFindColor(pressedColorName, out Color32 pressedColor))
        {
            Debug.LogError("Could not load theme color: " + pressedColorName);
            return;
        }
        colorBlock.pressedColor = pressedColor;

        selectable.colors = colorBlock;
    }
}
