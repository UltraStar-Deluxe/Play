using System;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public static class ApplyThemeStyleUtils
{
    private static readonly Dictionary<VisualElement, VisualElementData> visualElementToData = new();
    private static readonly Dictionary<VisualElement, VisualElement> listViewToSelectedVisualElement = new();

    public static void ClearCache()
    {
        visualElementToData.Clear();
        listViewToSelectedVisualElement.Clear();
    }

    public static bool TryApplyScaleMode(VisualElement visualElement, string scaleModeAsString)
    {
        if (!scaleModeAsString.IsNullOrEmpty()
            && Enum.TryParse(scaleModeAsString, out ScaleMode scaleMode))
        {
            visualElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(scaleMode);
            if (visualElement is Image image)
            {
                image.scaleMode = scaleMode;
            }
            return true;
        }
        return false;
    }

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

        if (visualElement.ClassListContains("staticPanel"))
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
            listView.selectionChanged += selectedObjects => OnListViewSelectionChanged(listView);
        }
    }

    public static void UpdateStylesOnListViewFocusChanged(ListViewH listView)
    {
        if (listView != null)
        {
            listView.RegisterCallback<FocusEvent>(evt => OnListViewFocusChanged(listView, true), TrickleDown.TrickleDown);
            listView.RegisterCallback<BlurEvent>(evt => OnListViewFocusChanged(listView, false), TrickleDown.TrickleDown);
        }
    }

    public static void UpdateStylesOnListViewSelectionChanged(ListViewH listView)
    {
        if (listView != null
            && !listViewToSelectedVisualElement.ContainsKey(listView))
        {
            VisualElement initialSelectedVisualElement = listView.GetSelectedVisualElement();
            listViewToSelectedVisualElement[listView] = initialSelectedVisualElement;
            listView.selectionChanged += selectedObjects => OnListViewSelectionChanged(listView);
        }
    }

    private static void OnListViewSelectionChanged(ListView listView)
    {
        VisualElement oldSelectedVisualElement = listViewToSelectedVisualElement[listView];
        SetListViewItemActive(listView, oldSelectedVisualElement, false);

        VisualElement newSelectedVisualElement = listView.GetSelectedVisualElement();
        if (newSelectedVisualElement != null)
        {
            SetListViewItemActive(listView, newSelectedVisualElement, true);
        }
    }

    private static void OnListViewFocusChanged(ListViewH listView, bool focused)
    {
        if (!listViewToSelectedVisualElement.TryGetValue(listView, out VisualElement oldSelectedVisualElement))
        {
            return;
        }

        VisualElement listItem = GetListViewItem(oldSelectedVisualElement);
        if (listItem != null
            && visualElementToData.TryGetValue(listItem, out VisualElementData listItemData))
        {
            // Apply active style only if ListView is focused
            listItemData.isActive = focused;
            UpdateStyles(listItemData);
        }
    }

    private static void OnListViewSelectionChanged(ListViewH listView)
    {
        if (!listViewToSelectedVisualElement.TryGetValue(listView, out VisualElement oldSelectedVisualElement))
        {
            return;
        }
        SetListViewItemActive(listView, oldSelectedVisualElement, false);

        VisualElement newSelectedVisualElement = listView.GetSelectedVisualElement();
        if (newSelectedVisualElement != null
            && VisualElementUtils.IsListViewFocused(listView))
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

        VisualElement listItem = GetListViewItem(listItemAncestor);
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

    public static void SetListViewItemActive(ListViewH listView, VisualElement listItemAncestor, bool isActive)
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
        else if (listViewToSelectedVisualElement.TryGetValue(listView, out VisualElement selectedListItemVisualElement)
                 && selectedListItemVisualElement == listItemAncestor)
        {
            listViewToSelectedVisualElement[listView] = null;
        }
    }

    private static VisualElement GetListViewItem(VisualElement listItemAncestor)
    {
        if (listItemAncestor == null)
        {
            return null;
        }

        VisualElement listItem = listItemAncestor.ClassListContains("listItem")
            ? listItemAncestor
            : listItemAncestor.Q(null, "listItem");
        return listItem;
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
                || !TimeUtils.IsDurationAboveThresholdInSeconds(data.initTimeInSeconds, 0.1f))
            {
                // Immediately apply the new gradient
                ApplyGradient(visualElement, newGradientConfig);
            }
            else
            {
                // Transition to new gradient
                AnimationUtils.TransitionBackgroundImageGradientAsync(
                    visualElement,
                    data.currentGradientConfig,
                    newGradientConfig,
                    0.2f);
            }
        }
        data.currentGradientConfig = newGradientConfig;
    }

    public static void ApplyGradient(VisualElement visualElement, GradientConfig newGradientConfig)
    {
        if (newGradientConfig == null)
        {
            visualElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
        }
        else
        {
            visualElement.style.backgroundImage = new StyleBackground(GradientManager.GetGradientTexture(newGradientConfig));
        }
    }

    private static async void ApplyStyle(VisualElementData data,
        Color32 fontColor,
        Color32 borderColor,
        Color32 backgroundColor,
        GradientConfig backgroundGradient,
        string backgroundImagePath,
        TextShadowConfig textShadowConfig)
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
                Sprite loadedSprite = await ImageManager.LoadSpriteFromUriAsync(absoluteBackgroundImagePath);
                visualElement.style.backgroundImage = new StyleBackground(loadedSprite);
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

        bool hasFontColor = !Equals(fontColor, default(Color32));
        if (hasFontColor)
        {
            // Set font color for child elements
            visualElement.style.color = new StyleColor(fontColor);
            if (visualElement is Button)
            {
                visualElement.Query<Label>()
                    .ForEach(label =>
                    {
                        if (IsIgnoredVisualElement(label)
                            || label.ClassListContains("warningFontColor")
                            || label.ClassListContains("errorFontColor"))
                        {
                            return;
                        }

                        label.style.color = new StyleColor(fontColor);
                    });
            }
            visualElement.style.color = new StyleColor(fontColor);
        }

        if (visualElement.childCount == 0)
        {
            // Set textShadow directly on element
            if (!visualElement.ClassListContains("noTextShadow")
                && !IsIgnoredVisualElement(visualElement))
            {
                ApplyTextShadow(visualElement, textShadowConfig);
            }
        }
        else
        {
            // Set textShadow for child labels
            visualElement.Query<Label>()
                .ForEach(label =>
                {
                    if (label.ClassListContains("noTextShadow")
                        || IsIgnoredVisualElement(label))
                    {
                        return;
                    }

                    if (label.ClassListContains("textShadow")
                        && (textShadowConfig == null || Equals(textShadowConfig.color, default(Color32))))
                    {
                        // Do not remove text shadow
                        return;
                    }

                    ApplyTextShadow(label, textShadowConfig);

                    if (hasFontColor
                        && !label.ClassListContains("warningFontColor")
                        && !label.ClassListContains("errorFontColor")
                        && !IsIgnoredVisualElement(label))
                    {
                        label.style.color = new StyleColor(fontColor);
                    }
                });
        }

        visualElement.SetBorderColor(borderColor);
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
            ControlStyleConfig slideToggleOnStyle = ThemeManager.Instance.GetCurrentTheme().ThemeJson.slideToggleOn;
            if (slideToggleOnStyle != null)
            {
                return slideToggleOnStyle;
            }
        }

        return data.controlStyleConfig;
    }

    private static void ApplyActiveStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.activeFontColor, c.focusFontColor, c.hoverFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.activeBorderColor, c.focusBorderColor, c.hoverBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.activeBackgroundColor, c.focusBackgroundColor, c.hoverBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.activeBackgroundGradient, c.focusBackgroundGradient, c.hoverBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.activeBackgroundImage, c.focusBackgroundImage, c.hoverBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.activeTextShadow, c.focusTextShadow, c.hoverTextShadow, c.textShadow));
    }

    private static void ApplyFocusStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.focusFontColor, c.activeFontColor, c.hoverFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.focusBorderColor, c.activeBorderColor, c.hoverBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.focusBackgroundColor, c.activeBackgroundColor, c.hoverBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.focusBackgroundGradient, c.activeBackgroundGradient, c.hoverBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.focusBackgroundImage, c.activeBackgroundImage, c.hoverBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.focusTextShadow, c.activeTextShadow, c.hoverTextShadow, c.textShadow));
    }

    private static void ApplyHoverFocusStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.hoverFocusFontColor, c.hoverActiveFontColor, c.focusFontColor, c.activeFontColor, c.hoverFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.hoverFocusBorderColor, c.hoverActiveBorderColor, c.focusBorderColor, c.activeBorderColor, c.hoverBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.hoverFocusBackgroundColor, c.hoverActiveBackgroundColor, c.focusBackgroundColor, c.activeBackgroundColor, c.hoverBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.hoverFocusBackgroundGradient, c.hoverActiveBackgroundGradient, c.focusBackgroundGradient, c.activeBackgroundGradient, c.hoverBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.hoverFocusBackgroundImage, c.hoverActiveBackgroundImage, c.focusBackgroundImage, c.activeBackgroundImage, c.hoverBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.hoverFocusTextShadow, c.hoverActiveTextShadow, c.focusTextShadow, c.activeTextShadow, c.hoverTextShadow, c.textShadow));
    }

    private static void ApplyHoverActiveStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.hoverActiveFontColor, c.hoverFocusFontColor, c.activeFontColor, c.focusFontColor, c.hoverFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.hoverActiveBorderColor, c.hoverFocusBorderColor, c.activeBorderColor, c.focusBorderColor, c.hoverBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.hoverActiveBackgroundColor, c.hoverFocusBackgroundColor, c.activeBackgroundColor, c.focusBackgroundColor, c.hoverBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.hoverActiveBackgroundGradient, c.hoverFocusBackgroundGradient, c.activeBackgroundGradient, c.focusBackgroundGradient, c.hoverBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.hoverActiveBackgroundImage, c.hoverFocusBackgroundImage, c.activeBackgroundImage, c.focusBackgroundImage, c.hoverBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.hoverActiveTextShadow, c.hoverFocusTextShadow, c.activeTextShadow, c.focusTextShadow, c.hoverTextShadow, c.textShadow));
    }

    private static void ApplyHoverStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.hoverFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.hoverBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.hoverBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.hoverBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.hoverBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.hoverTextShadow, c.textShadow));
    }

    private static void ApplyDefaultStyle(VisualElementData data)
    {
        ControlStyleConfig controlStyleConfig = GetControlStyleConfig(data);
        ApplyStyle(data,
            controlStyleConfig.fontColor,
            controlStyleConfig.borderColor,
            controlStyleConfig.backgroundColor,
            controlStyleConfig.backgroundGradient,
            controlStyleConfig.backgroundImage,
            controlStyleConfig.textShadow);
    }

    private static void ApplyDisabledStyle(VisualElementData data)
    {
        ControlStyleConfig c = GetControlStyleConfig(data);
        ApplyStyle(data,
            ObjectUtils.FirstNonDefault(c.disabledFontColor, c.fontColor),
            ObjectUtils.FirstNonDefault(c.disabledBorderColor, c.borderColor),
            ObjectUtils.FirstNonDefault(c.disabledBackgroundColor, c.backgroundColor),
            ObjectUtils.FirstNonDefault(c.disabledBackgroundGradient, c.backgroundGradient),
            ObjectUtils.FirstNonDefault(c.disabledBackgroundImage, c.backgroundImage),
            ObjectUtils.FirstNonDefault(c.disabledTextShadow, c.textShadow));
    }

    private static void UpdateStyles(VisualElementData data)
    {
        if (IsIgnoredVisualElement(data.visualElement))
        {
            return;
        }

        bool shouldApplyHoverStyle = data.isPointerOver;
        bool shouldApplyFocusStyle = data.hasFocus;
        bool shouldApplyActiveStyle = data.isActive;
        bool shouldApplyDisabledStyle = !data.visualElement.enabledInHierarchy;

        if (!shouldApplyHoverStyle && !shouldApplyFocusStyle && !shouldApplyActiveStyle)
        {
            ApplyDefaultStyle(data);
        }
        else if (shouldApplyDisabledStyle)
        {
            ApplyDisabledStyle(data);
        }
        else if (shouldApplyHoverStyle && shouldApplyFocusStyle)
        {
            ApplyHoverFocusStyle(data);
        }
        else if (shouldApplyHoverStyle && shouldApplyActiveStyle)
        {
            ApplyHoverActiveStyle(data);
        }
        else if (shouldApplyFocusStyle)
        {
            ApplyFocusStyle(data);
        }
        else if (shouldApplyActiveStyle)
        {
            ApplyActiveStyle(data);
        }
        else if (shouldApplyHoverStyle)
        {
            ApplyHoverStyle(data);
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
            root.Query(null, R_PlayShared.UssClasses.fontColorBorder)
                .ForEach(visualElement => visualElement.SetBorderColor(color));
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

    public static void ApplyNoBackgroundInHierarchyTextShadow(TextShadowConfig textShadowConfig, VisualElement root)
    {
        root.Query(null, "noBackgroundInHierarchy")
            .ForEach(visualElement =>
            {
                if (visualElement.ClassListContains("noTextShadow"))
                {
                    return;
                }

                if (visualElement is Label label)
                {
                    ApplyTextShadow(label, textShadowConfig);
                }
                else if (visualElement is Chooser chooser)
                {
                    ApplyTextShadow(chooser.LabelElement, textShadowConfig);
                }
                else
                {
                    visualElement.Query<Label>(null, "unity-base-field__label")
                        .ForEach(controlLabel => ApplyTextShadow(controlLabel, textShadowConfig));
                }
            });
    }

    public static void ApplyTextShadow(VisualElement visualElement, TextShadowConfig textShadowConfig)
    {
        if (textShadowConfig == null)
        {
            visualElement.style.textShadow = new StyleTextShadow();
            return;
        }

        TextShadow textShadow = new()
        {
            color = textShadowConfig.color,
            offset = textShadowConfig.offset,
            blurRadius = textShadowConfig.blurRadius,
        };
        visualElement.style.textShadow = textShadow;
    }
}
