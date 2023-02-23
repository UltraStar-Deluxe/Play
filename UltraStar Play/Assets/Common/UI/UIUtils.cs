using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIUtils
{
    public static void ForEachElementWithClass(VisualElement root, Action<VisualElement> callback, params string[] classNames)
    {
        foreach (string className in classNames)
        {
            root.Query(null, className).ForEach(callback);
        }
    }

    private static readonly Dictionary<VisualElement, ControlColorConfig> visualElementToControlColorConfig = new();

    public static void SetBackgroundStyleWithHoverAndFocus(VisualElement root, ControlColorConfig controlColorConfig)
    {
        SetBackgroundStyleWithHoverAndFocus(root, root, controlColorConfig);
    }

    public static void SetBackgroundStyleWithHoverAndFocus(VisualElement root, VisualElement hoverRoot, ControlColorConfig controlColorConfig)
    {
        if (root == null)
        {
            return;
        }
        bool isPointerOver = false;

        ControlColorConfig GetControlColorConfig()
        {
            return visualElementToControlColorConfig[hoverRoot];
        }
        
        bool HasFocus()
        {
            return hoverRoot.focusController.focusedElement == hoverRoot;
        }

        bool IsToggleButtonActive()
        {
            return hoverRoot is ToggleButton toggleButton
                   && toggleButton.IsActive;
        }

        void ApplyActiveToggleButtonStyle()
        {
            root.style.backgroundColor = GetControlColorConfig().activeToggleButtonColor;
        }
        
        void ApplyFocusStyle()
        {
            root.style.backgroundColor = GetControlColorConfig().focusBackgroundColor;
        }

        void ApplyHoverStyle()
        {
            root.style.backgroundColor = GetControlColorConfig().hoverBackgroundColor;
        }

        void ApplyDefaultStyle()
        {
            root.style.color = GetControlColorConfig().fontColor;
            root.style.backgroundColor = GetControlColorConfig().backgroundColor;
        }

        void UpdateStyles()
        {
            bool shouldApplyHoverStyle = isPointerOver;
            bool shouldApplyFocusStyle = HasFocus();
            bool shouldApplyActiveToggleButtonStyle = IsToggleButtonActive();
            if (!shouldApplyHoverStyle && !shouldApplyFocusStyle && !shouldApplyActiveToggleButtonStyle)
            {
                ApplyDefaultStyle();
            }
            else if (shouldApplyHoverStyle)
            {
                ApplyHoverStyle();
            }
            else if (shouldApplyFocusStyle)
            {
                ApplyFocusStyle();
            }
            else if (shouldApplyActiveToggleButtonStyle)
            {
                ApplyActiveToggleButtonStyle();
            }
        }

        // We can't access pseudo states through the API (e.g. :hover), so we have to manually mimic them.
        // But we do not want to register events multiple times.
        // However, the color config should be updated when called anew. This is why it is stored in a map.
        if (!visualElementToControlColorConfig.ContainsKey(hoverRoot))
        {
            hoverRoot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                isPointerOver = true;
                UpdateStyles();
            });
            hoverRoot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                isPointerOver = false;
                UpdateStyles();
            });

            hoverRoot.RegisterCallback<FocusEvent>(evt => UpdateStyles());
            hoverRoot.RegisterCallback<BlurEvent>(evt => UpdateStyles());
            if (hoverRoot is ToggleButton toggleButton)
            {
                toggleButton.IsActiveChangedEventStream.Subscribe(_ => UpdateStyles());
            }
        }
        visualElementToControlColorConfig[hoverRoot] = controlColorConfig;
        UpdateStyles();
    }

    private static bool IgnoreNonFocusNonHoverBackgroundColor(VisualElement root)
    {
        return root.ClassListContains("toggleButton");
    }

    public static void ApplyFontColorForElements(VisualElement root, string[] names, string[] classes, Color fontColor)
    {
        if (names == null)
        {
            root.Query(null, classes).ForEach(element => element.style.color = fontColor);
            return;
        }

        foreach (string name in names)
        {
            root.Query(name, classes).ForEach(element => element.style.color = fontColor);
        }
    }

    public static Color ColorHSVOffset(Color inputColor, float hueOffset, float saturationOffset, float valueOffset)
    {
        float h, s, v;
        Color.RGBToHSV(inputColor, out h, out s, out v);
        h += hueOffset;
        s += saturationOffset;
        v += valueOffset;
        return Color.HSVToRGB(h, s, v);
    }
}
