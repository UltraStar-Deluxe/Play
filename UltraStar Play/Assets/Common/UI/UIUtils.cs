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
        bool hasFocus = false;

        float initTimeInSeconds = Time.time;
        float transitionTimeInSeconds = 0.2f;
        GradientConfig currentGradientConfig = null;

        ControlColorConfig GetControlColorConfig()
        {
            return visualElementToControlColorConfig[hoverRoot];
        }

        void ApplyGradient(GradientConfig newGradientConfig)
        {
            if (newGradientConfig == null)
            {
                root.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }
            else
            {
                if (currentGradientConfig == null
                    || !TimeUtils.IsDurationAboveThreshold(initTimeInSeconds, 0.1f))
                {
                    // Immediately apply the new gradient
                    root.style.backgroundImage = new StyleBackground(GradientManager.GetGradientTexture(newGradientConfig));
                }
                else
                {
                    // Transition to new gradient
                    MainThreadDispatcher.StartCoroutine(AnimationUtils.TransitionBackgroundImageGradientCoroutine(
                        root,
                        currentGradientConfig,
                        newGradientConfig,
                        transitionTimeInSeconds));
                }
            }
            currentGradientConfig = newGradientConfig;
        }
        
        bool IsToggleButtonActive()
        {
            return hoverRoot is ToggleButton toggleButton
                   && toggleButton.IsActive;
        }

        void ApplyActiveStyle()
        {
            ControlColorConfig colorConfig = GetControlColorConfig();
            if (colorConfig.activeBackgroundGradient != null)
            {
                ApplyGradient(colorConfig.activeBackgroundGradient);
            }
            else
            {
                ApplyGradient(null);
                colorConfig.activeBackgroundColor.IfNotDefault(color => root.style.backgroundColor = color);
            }
            colorConfig.activeFontColor.IfNotDefault(color => root.style.color = color);
        }
        
        void ApplyFocusStyle()
        {
            ControlColorConfig colorConfig = GetControlColorConfig();
            if (colorConfig.focusBackgroundGradient != null)
            {
                ApplyGradient(colorConfig.focusBackgroundGradient);
            }
            else
            {
                ApplyGradient(null);
                colorConfig.focusBackgroundColor.IfNotDefault(color => root.style.backgroundColor = color);
            }
            colorConfig.focusFontColor.IfNotDefault(color => root.style.color = color);
        }

        void ApplyHoverStyle()
        {
            ControlColorConfig colorConfig = GetControlColorConfig();
            if (colorConfig.hoverBackgroundGradient != null)
            {
                ApplyGradient(colorConfig.hoverBackgroundGradient);
            }
            else
            {
                ApplyGradient(null);
                colorConfig.hoverBackgroundColor.IfNotDefault(color => root.style.backgroundColor = color);
            }
            colorConfig.hoverFontColor.IfNotDefault(color => root.style.color = color);
        }

        void ApplyDefaultStyle()
        {
            ControlColorConfig colorConfig = GetControlColorConfig();
            if (colorConfig.backgroundGradient != null)
            {
                ApplyGradient(colorConfig.backgroundGradient);
            }
            else
            {
                ApplyGradient(null);
                colorConfig.backgroundColor.IfNotDefault(color => root.style.backgroundColor = color);
            }
            colorConfig.fontColor.IfNotDefault(color => root.style.color = color);
        }

        void UpdateStyles()
        {
            bool shouldApplyHoverStyle = isPointerOver;
            bool shouldApplyFocusStyle = hasFocus;
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
                ApplyActiveStyle();
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

            hoverRoot.RegisterCallback<FocusEvent>(evt =>
            {
                hasFocus = true;
                UpdateStyles();
            });
            hoverRoot.RegisterCallback<BlurEvent>(evt =>
            {
                hasFocus = false;
                UpdateStyles();
            });
            
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
