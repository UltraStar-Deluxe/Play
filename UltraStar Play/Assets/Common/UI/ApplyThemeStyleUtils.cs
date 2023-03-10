using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class ApplyThemeStyleUtils
{
    private static readonly Dictionary<VisualElement, VisualElementData> visualElementToData = new();
    
    public static void ApplyControlStyles(VisualElement visualElement, ControlColorConfig controlColorConfig)
    {
        ApplyControlStyles(visualElement, visualElement, controlColorConfig);
    }

    public static void ApplyControlStyles(VisualElement visualElement, VisualElement callbackTarget, ControlColorConfig controlColorConfig)
    {
        if (visualElement == null)
        {
            return;
        }

        // We can't access pseudo states through the API (e.g. :hover), so we have to manually mimic them.
        // But we do not want to register events multiple times.
        // However, the color config should be updated when called anew. This is why it is stored in a map.
        if (!visualElementToData.TryGetValue(visualElement, out VisualElementData data))
        {
            data = new VisualElementData()
            {
                visualElement = visualElement,
                callbackTarget = callbackTarget,
                initTimeInSeconds = Time.time,
                controlColorConfig = controlColorConfig,
            };
            visualElementToData[visualElement] = data;
        }
        
        if (!data.hasRegisteredCallbacks)
        {
            data.hasRegisteredCallbacks = true;
            RegisterCallbacks(visualElement);
        }
        
        data.controlColorConfig = controlColorConfig;
        UpdateStyles(data);
    }

    private static void RegisterCallbacks(VisualElement visualElement)
    {
        visualElementToData.TryGetValue(visualElement, out VisualElementData data);
        if (data == null)
        {
            return;
        }
        VisualElement callbackTarget = data.callbackTarget;

        // Hover events
        callbackTarget.RegisterCallback<PointerEnterEvent>(evt =>
        {
            data.isPointerOver = true;
            UpdateStyles(data);
        });
        callbackTarget.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            data.isPointerOver = false;
            UpdateStyles(data);
        });

        // Focus events
        callbackTarget.RegisterCallback<FocusEvent>(evt =>
        {
            data.hasFocus = true;
            UpdateStyles(data);
        });
        callbackTarget.RegisterCallback<BlurEvent>(evt =>
        {
            data.hasFocus = false;
            UpdateStyles(data);
        });

        // Active events
        if (callbackTarget is ToggleButton toggleButton)
        {
            toggleButton.IsActiveChangedEventStream.Subscribe(isActive =>
            {
                data.isActive = isActive;
                UpdateStyles(data);
            });
        }
        else if (callbackTarget is SlideToggle slideToggle)
        {
            slideToggle.RegisterValueChangedCallback(evt =>
            {
                data.isActive = evt.newValue;
                UpdateStyles(data);
            });
        }
    }

    private static void ApplyGradient(VisualElementData data, GradientConfig newGradientConfig)
    {
        VisualElement visualElement = data.visualElement;
        if (newGradientConfig == null)
        {
            visualElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
        }
        else
        {
            if (data.currentGradientConfig == null
                || !TimeUtils.IsDurationAboveThreshold(data.initTimeInSeconds, 0.1f))
            {
                // Immediately apply the new gradient
                visualElement.style.backgroundImage = new StyleBackground(GradientManager.GetGradientTexture(newGradientConfig));
            }
            else
            {
                // Transition to new gradient
                MainThreadDispatcher.StartCoroutine(AnimationUtils.TransitionBackgroundImageGradientCoroutine(
                    visualElement,
                    data.currentGradientConfig,
                    newGradientConfig,
                    0.2f));
            }
        }
        data.currentGradientConfig = newGradientConfig;
    }

    private static void ApplyStyle(VisualElementData data,
        Color32 fontColor,
        Color32 backgroundColor,
        GradientConfig backgroundGradient,
        string backgroundImage)
    {
        VisualElement visualElement = data.visualElement;
        if (!backgroundImage.IsNullOrEmpty()
            && File.Exists(backgroundImage))
        {
            ImageManager.LoadSpriteFromFile(backgroundImage,
                loadedSprite => visualElement.style.backgroundImage = new StyleBackground(loadedSprite));
        }
        else if (backgroundGradient != null)
        {
            ApplyGradient(data, backgroundGradient);
        }
        else
        {
            ApplyGradient(data, null);
            backgroundColor.IfNotDefault(color => visualElement.style.backgroundColor = new StyleColor(color));
        }
        fontColor.IfNotDefault(color => visualElement.style.color = new StyleColor(color));
    }
    
    private static void ApplyActiveStyle(VisualElementData data)
    {
        ApplyStyle(data,
            data.controlColorConfig.activeFontColor,
            data.controlColorConfig.activeBackgroundColor,
            data.controlColorConfig.activeBackgroundGradient,
            data.controlColorConfig.activeBackgroundImage);
    }

    private static void ApplyFocusStyle(VisualElementData data)
    {
        ApplyStyle(data,
            data.controlColorConfig.focusFontColor,
            data.controlColorConfig.focusBackgroundColor,
            data.controlColorConfig.focusBackgroundGradient,
            data.controlColorConfig.focusBackgroundImage);
    }

    private static void ApplyHoverStyle(VisualElementData data)
    {
        ApplyStyle(data,
            data.controlColorConfig.hoverFontColor,
            data.controlColorConfig.hoverBackgroundColor,
            data.controlColorConfig.hoverBackgroundGradient,
            data.controlColorConfig.hoverBackgroundImage);
    }

    private static void ApplyDefaultStyle(VisualElementData data)
    {
        ApplyStyle(data,
            data.controlColorConfig.fontColor,
            data.controlColorConfig.backgroundColor,
            data.controlColorConfig.backgroundGradient,
            data.controlColorConfig.backgroundImage);
    }
    
    private static void ApplyDisabledStyle(VisualElementData data)
    {
        ApplyStyle(data,
            data.controlColorConfig.disabledFontColor,
            data.controlColorConfig.disabledBackgroundColor,
            data.controlColorConfig.disabledBackgroundGradient,
            data.controlColorConfig.disabledBackgroundImage);
    }

    private static void UpdateStyles(VisualElementData data)
    {
        bool shouldApplyHoverStyle = data.isPointerOver;
        bool shouldApplyFocusStyle = data.hasFocus;
        bool shouldApplyActiveStyle = data.isActive;
        bool shouldApplyDisabledStyle = !data.visualElement.enabledInHierarchy;

        if (!shouldApplyHoverStyle && !shouldApplyFocusStyle && !shouldApplyActiveStyle && !shouldApplyDisabledStyle)
        {
            ApplyDefaultStyle(data);
        }
        else if (shouldApplyDisabledStyle)
        {
            ApplyDisabledStyle(data);
        }
        else if (shouldApplyHoverStyle)
        {
            ApplyHoverStyle(data);
        }
        else if (shouldApplyFocusStyle)
        {
            ApplyFocusStyle(data);
        }
        else if (shouldApplyActiveStyle)
        {
            ApplyActiveStyle(data);
        }
    }

    private class VisualElementData
    {
        public VisualElement visualElement;
        public VisualElement callbackTarget;
        public bool hasRegisteredCallbacks;
        public bool isPointerOver;
        public bool hasFocus;
        public bool isActive;
        
        public float initTimeInSeconds;
        public ControlColorConfig controlColorConfig;
        public GradientConfig currentGradientConfig;
    }
}
