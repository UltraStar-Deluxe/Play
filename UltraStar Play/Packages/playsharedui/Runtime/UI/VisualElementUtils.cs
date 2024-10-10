using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public static class VisualElementUtils
{
    // UIToolkit TextFields have a character limit imposed by the limit of vertices Unity provides for a VisualElement.
    // This limitation is planned to be removed in future versions of UIToolkit.
    // See https://forum.unity.com/threads/textfield-character-limit-text-will-be-truncated-because-it-exceeds-49152-vertices.1309179/#post-8281530
    public const int TextFieldCharacterLimit = 12000;

    public static void MoveVisualElementFullyInsideScreen(
        VisualElement visualElement,
        PanelHelper panelHelper,
        float marginLeft = 0,
        float marginRight = 0,
        float marginTop = 0,
        float marginBottom = 0)
    {
        Vector2 screenSizeInPanelCoordinates = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
        float overshootRight = (visualElement.worldBound.xMax + marginRight) - screenSizeInPanelCoordinates.x;
        float overshootLeft = (visualElement.worldBound.xMin - marginLeft);
        float overshootBottom = (visualElement.worldBound.yMax + marginBottom) - screenSizeInPanelCoordinates.y;
        float overshootTop = (visualElement.worldBound.yMin - marginTop);

        Vector2 shift = Vector2.zero;
        if (overshootLeft < 0)
        {
            shift = new Vector2(overshootLeft, shift.y);
        }
        if (overshootRight > 0)
        {
            shift = new Vector2(overshootRight, shift.y);
        }

        if (overshootTop < 0)
        {
            shift = new Vector2(shift.x, overshootTop);
        }
        if (overshootBottom > 0)
        {
            shift = new Vector2(shift.x, overshootBottom);
        }

        if (shift == Vector2.zero)
        {
            return;
        }

        if (shift.x != 0)
        {
            if (visualElement.style.left != new StyleLength(StyleKeyword.Null)
                && visualElement.style.left != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.left = visualElement.style.left.value.value - shift.x;
            }
            else if (visualElement.style.right != new StyleLength(StyleKeyword.Null)
                     && visualElement.style.right != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.right = visualElement.style.right.value.value + shift.x;
            }
        }

        if (shift.y != 0)
        {
            if (visualElement.style.top != new StyleLength(StyleKeyword.Null)
                && visualElement.style.top != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.top = visualElement.style.top.value.value - shift.y;
            }
            else if (visualElement.style.bottom != new StyleLength(StyleKeyword.Null)
                     && visualElement.style.bottom != new StyleLength(StyleKeyword.Auto))
            {
                visualElement.style.top = visualElement.style.bottom.value.value + shift.y;
            }
        }
    }

    public static VisualElement GetFocusedVisualElement(FocusController focusController)
    {
        if (focusController == null)
        {
            return null;
        }

        VisualElement focusedVisualElement = focusController.focusedElement as VisualElement;
        return focusedVisualElement;
    }

    public static bool IsDropdownListFocused(FocusController focusController)
    {
        return IsDropdownListFocused(focusController, out VisualElement _);
    }

    public static bool IsDropdownListFocused(FocusController focusController, out VisualElement unityBaseDropdown)
    {
        VisualElement focusedVisualElement = GetFocusedVisualElement(focusController);
        if (focusedVisualElement == null
            || !focusedVisualElement.ClassListContains("unity-base-dropdown__container-inner"))
        {
            unityBaseDropdown = null;
            return false;
        }

        unityBaseDropdown = focusedVisualElement.GetParent(parent => parent.ClassListContains("unity-base-dropdown"));
        return unityBaseDropdown != null;
    }

    public static void RegisterDirectClickCallback(VisualElement visualElement, Action onDirectClick = null)
    {
        visualElement.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.target == visualElement)
            {
                onDirectClick();
            }
        });
    }

    public static void RegisterCallbackToHideByDisplayOnDirectClick(VisualElement visualElement)
    {
        RegisterDirectClickCallback(visualElement, visualElement.HideByDisplay);
    }

    public static bool HasGeometry(VisualElement visualElement)
    {
        if (visualElement == null)
        {
            return false;
        }

        Rect worldBound = visualElement.worldBound;
        return !float.IsNaN(worldBound.width)
               && !float.IsNaN(worldBound.height);
    }

    public static bool HasGeometryAndNonZeroSize(VisualElement visualElement)
    {
        if (visualElement == null)
        {
            return false;
        }

        Rect worldBound = visualElement.worldBound;
        return !float.IsNaN(worldBound.width)
               && !float.IsNaN(worldBound.height)
               && worldBound.width > 0
               && worldBound.height > 0;
    }

    public static VisualElement LoadVisualElementFromResources(string path)
    {
        VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>(path);
        if (visualTreeAsset == null)
        {
            throw new Exception("Could not load " + path);
        }
        return visualTreeAsset.CloneTree().Children().FirstOrDefault();
    }

    public static Rect WorldBoundToLocalBound(Label visualElement, Rect worldRect)
    {
        if (visualElement.parent == null)
        {
            return worldRect;
        }

        Rect parentWorldRect = visualElement.parent.worldBound;
        return new Rect(worldRect.x - parentWorldRect.x,
            worldRect.y - parentWorldRect.y,
            worldRect.width,
            worldRect.height);
    }

    public static bool IsDescendantFocused(VisualElement visualElement)
    {
        VisualElement focusedVisualElement = GetFocusedVisualElement(visualElement.focusController);
        VisualElement matchingParentOfFocusedVisualElement = focusedVisualElement.GetParent(parent => parent == visualElement);
        return matchingParentOfFocusedVisualElement != null;
    }

    public static bool IsNonStyleKeywordValueSet(StyleLength style)
    {
        return style != new StyleLength(StyleKeyword.Null)
               && style != new StyleLength(StyleKeyword.Auto)
               && style != new StyleLength(StyleKeyword.Initial)
               && style != new StyleLength(StyleKeyword.None);
    }

    public static bool IsNonStyleKeywordValueSet(StyleBackground style)
    {
        return style != new StyleBackground(StyleKeyword.Null)
               && style != new StyleBackground(StyleKeyword.Auto)
               && style != new StyleBackground(StyleKeyword.Initial)
               && style != new StyleBackground(StyleKeyword.None);
    }

    public static bool IsNonStyleKeywordValueSet(StyleColor style)
    {
        return style != new StyleColor(StyleKeyword.Null)
               && style != new StyleColor(StyleKeyword.Auto)
               && style != new StyleColor(StyleKeyword.Initial)
               && style != new StyleColor(StyleKeyword.None);
    }

    public static VisualElement GetElementUnderPointer(UIDocument uiDocument, PanelHelper panelHelper)
    {
        if (Pointer.current == null)
        {
            return null;
        }

        Vector2 pointerPanelPos = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true);
        VisualElement picked = uiDocument.rootVisualElement?.panel?.Pick(pointerPanelPos);
        return picked;
    }

    public static bool IsFocusableNow(VisualElement visualElement, UIDocument uiDocument)
    {
        if (visualElement == null)
        {
            return false;
        }
        Rect worldBound = visualElement.worldBound;
        return visualElement.IsVisibleByDisplay()
               && !float.IsNaN(worldBound.center.x)
               && !float.IsNaN(worldBound.center.y)
               && worldBound.height > 0
               && worldBound.width > 0
               && visualElement is not Focusable { focusable: false }
               && visualElement.enabledInHierarchy
               && visualElement.canGrabFocus
               && !visualElement.ClassListContains("focusableNavigatorIgnore")
               && IsAllAncestorsFocusableNow(visualElement, uiDocument);
    }

    private static bool IsAllAncestorsFocusableNow(VisualElement visualElement, UIDocument uiDocument)
    {
        List<VisualElement> ancestors = visualElement.GetAncestors();

        bool isInHierarchy = ancestors.AnyMatch(ancestor => ancestor == uiDocument.rootVisualElement);
        if (!isInHierarchy)
        {
            return false;
        }

        return ancestors.AllMatch(ancestor =>
        {
            Rect ancestorWorldBound = ancestor.worldBound;
            return ancestor.IsVisibleByDisplay()
                   && !float.IsNaN(ancestorWorldBound.center.x)
                   && !float.IsNaN(ancestorWorldBound.center.y)
                   && !float.IsNaN(ancestorWorldBound.width)
                   && !float.IsNaN(ancestorWorldBound.height)
                   && ancestorWorldBound.width > 0
                   && ancestorWorldBound.height > 0
                   && ancestor.enabledInHierarchy
                   && !ancestor.ClassListContains("focusableNavigatorIgnore")
                   && !ancestor.ClassListContains(VisualElementSlideInControl.SlideOutClassName);
        }) ;
    }

    public static void SetAbsoluteWorldBoundPosition(VisualElement element, Vector2 pos)
    {
        element.style.left = pos.x - (element.parent != null ? element.parent.worldBound.xMin : 0);
        element.style.top = pos.y - (element.parent != null ? element.parent.worldBound.yMin : 0);
    }
}
