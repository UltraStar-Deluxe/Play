using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class ApplyThemeStyleUtils
{
    private static readonly Dictionary<VisualElement, VisualElementData> visualElementToData = new();
    private static readonly Dictionary<ListView, VisualElement> listViewToSelectedVisualElement = new();
    
    public static void ApplyControlStyles(VisualElement visualElement, VisualElement styleTarget, ControlStyleConfig controlStyleConfig)
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
                styleTarget = styleTarget,
                initTimeInSeconds = Time.time,
                controlStyleConfig = controlStyleConfig,
                hasFocus = visualElement.focusController?.focusedElement == visualElement,
            };
            visualElementToData[visualElement] = data;
        }
        
        if (!data.hasRegisteredCallbacks)
        {
            data.hasRegisteredCallbacks = true;
            RegisterCallbacks(visualElement);
        }
        
        data.controlStyleConfig = controlStyleConfig;
        UpdateStyles(data);
    }

    private static void RegisterCallbacks(VisualElement visualElement)
    {
        visualElementToData.TryGetValue(visualElement, out VisualElementData data);
        if (data == null)
        {
            return;
        }

        // Hover events
        visualElement.RegisterCallback<PointerEnterEvent>(evt =>
        {
            data.isPointerOver = true;
            UpdateStyles(data);
        });
        visualElement.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            data.isPointerOver = false;
            UpdateStyles(data);
        });

        // Focus events
        visualElement.RegisterCallback<FocusEvent>(evt =>
        {
            data.hasFocus = true;
            UpdateStyles(data);
        });
        visualElement.RegisterCallback<BlurEvent>(evt =>
        {
            data.hasFocus =false;
            UpdateStyles(data);
        });

        // Active events
        if (visualElement is ToggleButton toggleButton)
        {
            toggleButton.IsActiveChangedEventStream.Subscribe(isActive =>
            {
                data.isActive = isActive;
                UpdateStyles(data);
            });
        }
        else if (visualElement is SlideToggle slideToggle)
        {
            slideToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateStyles(data);
            });
        }
    }

    public static void UpdateStylesOnListViewSelectionChanged(ListView listView)
    {
        if (listView != null
            && !listViewToSelectedVisualElement.ContainsKey(listView))
        {
            VisualElement initialSelectedVisualElement = listView.GetSelectedVisualElement();
            listViewToSelectedVisualElement[listView] = initialSelectedVisualElement;
            listView.selectionChanged += selectedObjects => OnListViewSelectionChanged(listView, selectedObjects);
        }
    }

    private static void OnListViewSelectionChanged(ListView listView, IEnumerable<object> selectedObjects)
    {
        VisualElement oldSelectedVisualElement = listViewToSelectedVisualElement[listView];
        SetListViewItemActive(listView, oldSelectedVisualElement, false);

        VisualElement newSelectedVisualElement = listView.GetSelectedVisualElement();
        if (newSelectedVisualElement != null)
        {
            SetListViewItemActive(listView, newSelectedVisualElement, true);
        }
    }

    public static void SetListViewItemActive(ListView listView, VisualElement listItemAncestor, bool isActive)
    {
        if (listItemAncestor == null)
        {
            return;
        }
        
        VisualElement listItem = listItemAncestor.ClassListContains("listItem")
            ? listItemAncestor
            : listItemAncestor.Q(null, "listItem");
        if (listItem != null
            && visualElementToData.TryGetValue(listItem, out VisualElementData listItemData))
        {
            listItemData.isActive = isActive;
            UpdateStyles(listItemData);
        }

        if (isActive)
        {
            listViewToSelectedVisualElement[listView] = listItemAncestor;
        }
        else if (listViewToSelectedVisualElement[listView] == listItemAncestor)
        {
            listViewToSelectedVisualElement[listView] = null;
        }
    }

    private static void ApplyGradient(VisualElementData data, GradientConfig newGradientConfig)
    {
        VisualElement visualElement = data.styleTarget;
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
        Color32 borderColor,
        Color32 backgroundColor,
        GradientConfig backgroundGradient,
        string backgroundImagePath)
    {
        VisualElement visualElement = data.styleTarget;
        if (IsIgnoredVisualElement(visualElement))
        {
            return;
        }
        
        if (!backgroundImagePath.IsNullOrEmpty())
        {
            string absoluteBackgroundImagePath = PathUtils.IsAbsolutePath(backgroundImagePath)
                ? backgroundImagePath
                : ThemeMetaUtils.GetAbsoluteFilePath(ThemeManager.Instance.GetCurrentTheme(), backgroundImagePath);
            if (File.Exists(absoluteBackgroundImagePath))
            {
                ImageManager.LoadSpriteFromFile(absoluteBackgroundImagePath,
                    loadedSprite => visualElement.style.backgroundImage = new StyleBackground(loadedSprite));
                visualElement.style.backgroundColor = new StyleColor(StyleKeyword.None);
            }
            else
            {
                Debug.LogWarning($"Could not find background image at path {absoluteBackgroundImagePath}");
            }
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

        visualElement.SetBorderColor(borderColor);
        fontColor.IfNotDefault(color =>
        {
            visualElement.style.color = new StyleColor(color);
            visualElement.Query<Label>()
                .ForEach(label =>
                {
                    if (IsIgnoredVisualElement(label))
                    {
                        return;
                    }
                    label.style.color = new StyleColor(color);
                });
        });
    }

    public static bool IsIgnoredVisualElement(VisualElement visualElement)
    {
        return visualElement.ClassListContains("ignoreTheme");
    }
    
    private static ControlStyleConfig GetControlStyleConfig(VisualElementData data)
    {
        if (data.visualElement is SlideToggle slideToggle
            && slideToggle.value)
        {
            // Dirty hack to get the correct style for the slide toggle
            return ThemeManager.Instance.GetCurrentTheme().ThemeJson.slideToggleOn;
        }

        return data.controlStyleConfig;
    }
    
    private static void ApplyActiveStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.activeFontColor,
            controlStyleConfig.activeBorderColor,
            controlStyleConfig.activeBackgroundColor,
            controlStyleConfig.activeBackgroundGradient,
            controlStyleConfig.activeBackgroundImage);
    }

    private static void ApplyFocusStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.focusFontColor,
            controlStyleConfig.focusBorderColor,
            controlStyleConfig.focusBackgroundColor,
            controlStyleConfig.focusBackgroundGradient,
            controlStyleConfig.focusBackgroundImage);
    }

    private static void ApplyHoverStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.hoverFontColor,
            controlStyleConfig.hoverBorderColor,
            controlStyleConfig.hoverBackgroundColor,
            controlStyleConfig.hoverBackgroundGradient,
            controlStyleConfig.hoverBackgroundImage);
    }

    private static void ApplyDefaultStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.fontColor,
            controlStyleConfig.borderColor,
            controlStyleConfig.backgroundColor,
            controlStyleConfig.backgroundGradient,
            controlStyleConfig.backgroundImage);
    }
    
    private static void ApplyDisabledStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.disabledFontColor,
            controlStyleConfig.disabledBorderColor,
            controlStyleConfig.disabledBackgroundColor,
            controlStyleConfig.disabledBackgroundGradient,
            controlStyleConfig.disabledBackgroundImage);
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
        public VisualElement styleTarget;
        public bool hasRegisteredCallbacks;
        public bool isPointerOver;
        public bool hasFocus;
        public bool isActive;
        
        public float initTimeInSeconds;
        public ControlStyleConfig controlStyleConfig;
        public GradientConfig currentGradientConfig;
    }

    public static void ApplyPrimaryFontColor(Color32 fontColor, VisualElement root)
    {
        fontColor.IfNotDefault(color =>
        {
            root.Query<Label>()
                .Where(label => !label.ClassListContains("secondaryFontColor")
                                && !label.ClassListContains("warningFontColor")
                                && !label.ClassListContains("errorFontColor")
                                && !IsIgnoredVisualElement(label))
                .ForEach(label => label.style.color = new StyleColor(color));
            root.Query(null, R.UssClasses.fontColor)
                .ForEach(visualElement => visualElement.style.unityBackgroundImageTintColor = new StyleColor(color));
        });
    }

    public static void ApplySecondaryFontColor(Color32 fontColor, VisualElement root)
    {
        fontColor.IfNotDefault(color =>
        {
            root.Query<Label>()
                .Where(label => label.ClassListContains("secondaryFontColor")
                                && !label.ClassListContains("warningFontColor")
                                && !label.ClassListContains("errorFontColor")
                                && !IsIgnoredVisualElement(label))
                .ForEach(label => label.style.color = new StyleColor(color));
        });
    }

    public static void ApplyWarningFontColor(Color32 fontColor, VisualElement root)
    {
        fontColor.IfNotDefault(color =>
        {
            root.Query<Label>()
                .Where(label => label.ClassListContains("warningFontColor")
                                && !IsIgnoredVisualElement(label))
                .ForEach(label => label.style.color = new StyleColor(color));
        });
    }
    
    public static void ApplyErrorFontColor(Color32 fontColor, VisualElement root)
    {
        fontColor.IfNotDefault(color =>
        {
            root.Query<Label>()
                .Where(label => label.ClassListContains("errorFontColor")
                                && !IsIgnoredVisualElement(label))
                .ForEach(label => label.style.color = new StyleColor(color));
        });
    }
}
