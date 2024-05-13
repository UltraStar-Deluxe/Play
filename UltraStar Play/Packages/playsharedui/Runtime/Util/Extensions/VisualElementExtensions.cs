using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public static class VisualElementExtensions
{
    public static void SetValueIfChanged<T>(this BaseField<T> baseField, T newValue)
    {
        if (!Equals(baseField.value, newValue))
        {
            baseField.value = newValue;
        }
    }

    public static void RegisterCallbackButtonTriggered(this Button button, EventCallback<EventBase> callback, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
    {
        button.RegisterCallback<ClickEvent>(callback, trickleDown);
        button.RegisterCallback<NavigationSubmitEvent>(callback, trickleDown);
    }

    public static void UnregisterCallbackButtonTriggered(this Button button, EventCallback<EventBase> callback, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
    {
        button.UnregisterCallback<ClickEvent>(callback, trickleDown);
        button.UnregisterCallback<NavigationSubmitEvent>(callback, trickleDown);
    }

    public static void AddToClassListIfNew(this VisualElement visualElement, params string[] newClasses)
    {
        HashSet<string> currentClasses = new();
        visualElement.GetClasses().ForEach(currentClass => currentClasses.Add(currentClass));
        newClasses.ForEach(newClass =>
        {
            if (!currentClasses.Contains(newClass))
            {
                visualElement.AddToClassList(newClass);
            }
        });
    }

    public static void SetInClassList(this VisualElement visualElement, string className, bool shouldBePresent)
    {
        if (shouldBePresent)
        {
            if (!visualElement.ClassListContains(className))
            {
                visualElement.AddToClassList(className);
            }
        }
        else
        {
            if (visualElement.ClassListContains(className))
            {
                visualElement.RemoveFromClassList(className);
            }
        }
    }

    public static bool IsVisibleByDisplay(this VisualElement visualElement)
    {
        return visualElement.resolvedStyle.display != DisplayStyle.None;
    }

    public static void SetVisibleByDisplay(this VisualElement visualElement, bool isVisible)
    {
        if (isVisible)
        {
            visualElement.ShowByDisplay();
        }
        else
        {
            visualElement.HideByDisplay();
        }
    }

    public static void ToggleVisibleByDisplay(this VisualElement visualElement)
    {
        visualElement.SetVisibleByDisplay(!visualElement.IsVisibleByDisplay());
    }

    public static void ShowByDisplay(this VisualElement visualElement)
    {
        visualElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
    }

    public static void HideByDisplay(this VisualElement visualElement)
    {
        visualElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
    }

    public static bool IsVisibleByVisibility(this VisualElement visualElement)
    {
        return visualElement.style.visibility != Visibility.Hidden;
    }

    public static void SetVisibleByVisibility(this VisualElement visualElement, bool isVisible)
    {
        if (isVisible)
        {
            visualElement.ShowByVisibility();
        }
        else
        {
            visualElement.HideByVisibility();
        }
    }

    public static void ToggleVisibleByVisibility(this VisualElement visualElement)
    {
        visualElement.SetVisibleByVisibility(!visualElement.IsVisibleByVisibility());
    }

    public static void ShowByVisibility(this VisualElement visualElement)
    {
        visualElement.style.visibility = new StyleEnum<Visibility>(Visibility.Visible);
    }

    public static void HideByVisibility(this VisualElement visualElement)
    {
        visualElement.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
    }

    /**
     * Executes the given callback at most once when the event occurs.
     */
    public static void RegisterCallbackOneShot<TEventType>(
        this VisualElement visualElement,
        EventCallback<TEventType> callback,
        TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
        where TEventType : EventBase<TEventType>, new()
    {
        bool wasExecuted = false;
        void RunCallbackIfNotDoneYet(TEventType evt)
        {
            if (!wasExecuted)
            {
                wasExecuted = true;
                callback(evt);
                visualElement.UnregisterCallback<TEventType>(RunCallbackIfNotDoneYet, useTrickleDown);
            }
        }

        visualElement.RegisterCallback<TEventType>(RunCallbackIfNotDoneYet, useTrickleDown);
    }

    public static void RegisterHasGeometryCallbackOneShot(this VisualElement visualElement, EventCallback<GeometryChangedEvent> callback)
    {
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!float.IsNaN(visualElement.worldBound.width)
                && !float.IsNaN(visualElement.worldBound.height))
            {
                callback(evt);
                visualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        visualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public static void SetBackgroundImageAlpha(this VisualElement visualElement, float newAlpha)
    {
        Color lastColor = visualElement.resolvedStyle.unityBackgroundImageTintColor;
        visualElement.style.unityBackgroundImageTintColor = new Color(lastColor.r, lastColor.g, lastColor.b, newAlpha);
    }

    // Make the color of an image darker with a factor < 1, or brighter with a factor > 1.
    public static void MultiplyBackgroundImageColor(this VisualElement visualElement, float factor, bool includeAlpha = false)
    {
        Color lastColor = visualElement.resolvedStyle.unityBackgroundImageTintColor;
        float newR = NumberUtils.Limit(lastColor.r * factor, 0, 1);
        float newG = NumberUtils.Limit(lastColor.g * factor, 0, 1);
        float newB = NumberUtils.Limit(lastColor.b * factor, 0, 1);
        float newAlpha = includeAlpha ? NumberUtils.Limit(lastColor.a * factor, 0, 1) : lastColor.a;
        visualElement.style.unityBackgroundImageTintColor = new Color(newR, newG, newB, newAlpha);
    }

    public static void ScrollToSelf(this VisualElement visualElement)
    {
        if (visualElement == null)
        {
            return;
        }

        List<ScrollView> ancestorScrollViews = visualElement
            .GetAncestors()
            .OfType<ScrollView>()
            .ToList();
        ancestorScrollViews.ForEach(scrollView => scrollView.ScrollTo(visualElement));
    }

    public static List<VisualElement> GetAncestors(this VisualElement visualElement)
    {
        if (visualElement == null)
        {
            return new List<VisualElement>();
        }

        List<VisualElement> ancestors = new();
        VisualElement parent = visualElement.parent;
        while (parent != null)
        {
            ancestors.Add(parent);
            parent = parent.parent;
        }
        return ancestors;
    }

    public static void SetBorderColor(this VisualElement visualElement, Color color)
    {
        visualElement.style.borderLeftColor = color;
        visualElement.style.borderRightColor = color;
        visualElement.style.borderTopColor = color;
        visualElement.style.borderBottomColor = color;
    }

    public static void SetBorderWidth(this VisualElement visualElement, StyleFloat value)
    {
        visualElement.style.borderLeftWidth = value;
        visualElement.style.borderRightWidth = value;
        visualElement.style.borderTopWidth = value;
        visualElement.style.borderBottomWidth = value;
    }

    public static void SetBorderRadius(this VisualElement visualElement, StyleLength value)
    {
        visualElement.style.borderTopLeftRadius = value;
        visualElement.style.borderTopRightRadius = value;
        visualElement.style.borderBottomLeftRadius = value;
        visualElement.style.borderBottomRightRadius = value;
    }

    public static void SetSelectionAndScrollTo(this ListView listView, int index)
    {
        listView.SetSelection(index);
        listView.ScrollToItem(index);
    }

    public static void SetSelectionAndScrollTo(this ListViewH listView, int index)
    {
        listView.SetSelection(index);
        // listView.ScrollToItem(index);
    }

    public static VisualElement GetSelectedVisualElement(this ListView listView)
    {
        return listView.Q<VisualElement>(className: "unity-collection-view__item--selected");
    }

    public static VisualElement GetSelectedVisualElement(this ListViewH listView)
    {
        return listView.Q<VisualElement>(className: "unity-collection-view__item--selected");
    }

    public static VisualElement GetRootVisualElement(this VisualElement visualElement)
    {
        return visualElement.GetParent(parent => parent.parent == null
                                                 || parent.ClassListContains("unity-ui-document__root"));
    }

    public static VisualElement GetParent(this VisualElement visualElement, Func<VisualElement, bool> condition=null)
    {
        if (visualElement == null)
        {
            return null;
        }

        VisualElement parent = visualElement.parent;
        while (parent != null)
        {
            if (condition == null
                || condition(parent))
            {
                return parent;
            }
            parent = parent.parent;
        }

        return null;
    }

    public static List<VisualElement> GetParents(VisualElement visualElement)
    {
        List<VisualElement> parents = new();
        VisualElement parent = visualElement.parent;
        while (parent != null)
        {
            parents.Add(parent);
            parent = parent.parent;
        }

        return parents;
    }

    public static void RemoveTemplateContainers(this VisualElement visualElement)
    {
        visualElement
            .Query<TemplateContainer>()
            // Copy list to avoid modification while iterating
            .ToList()
            .ForEach(templateContainer => templateContainer.RemoveFromHierarchy());
    }

    public static void AddAsFirstChild(this VisualElement visualElement, VisualElement otherVisualElement)
    {
        visualElement.Add(otherVisualElement);
        otherVisualElement.SendToBack();
    }

    public static VisualElement CloneTreeAndGetFirstChild(this VisualTreeAsset visualTreeAsset)
    {
        return visualTreeAsset.CloneTree().Children().FirstOrDefault();
    }

    public static Vector2 GetPreferredTextSize(this Label label, string text = null)
    {
        if (text == null)
        {
            text = label.text;
        }
        return label.MeasureTextSize(label.text,
            0, VisualElement.MeasureMode.Undefined,
            0, VisualElement.MeasureMode.Undefined);
    }

    public static void DisableParseEscapeSequences(this TextField textField)
    {
        // See https://forum.unity.com/threads/preventing-escaped-characters-in-textfield.1071425/
        TextElement textElement = textField.Q<TextElement>();
        if (textElement == null)
        {
            return;
        }

        textElement.parseEscapeSequences = false;
    }

    public static void DisableChangeValueByDragging(this IntegerField visualElement)
    {
        // See https://forum.unity.com/threads/disable-integerfield-changing-value-on-drag.1448113/#post-9079210
        visualElement.labelElement.style.cursor = StyleKeyword.Initial;

        visualElement.labelElement.RegisterCallback<MouseMoveEvent>(
            e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);

        visualElement.labelElement.RegisterCallback<PointerMoveEvent>(
            e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
    }

    public static string ToUxml(this VisualElement root, int indentCount = 0)
    {
        string GetIndentation()
        {
            if (indentCount < 0)
            {
                return "";
            }

            StringBuilder sb = new();
            for (int i = 0; i < indentCount; i++)
            {
                sb.Append("    ");
            }
            return sb.ToString();
        }

        string indent = GetIndentation();

        StringBuilder sb = new();
        sb.Append(indent);
        sb.Append($"<{root.GetType().Name} name=\"{root.name}\" class=\"{root.GetClasses().JoinWith(" ")}\"");
        if (root.childCount > 0)
        {
            sb.Append(">\n");
            sb.Append(indent);
            sb.Append(root.Children()
                .Select(child => child.ToUxml(indentCount >= 0 ? indentCount + 1 : indentCount))
                .JoinWith($"\n{indent}"));
            if (indentCount == 0)
            {

                sb.Append("\n");
            }
            sb.Append($"</{root.GetType().Name}>");
        }
        else
        {
            sb.Append("/>");
        }
        return sb.ToString();
    }

    public static void Click(this Button button)
    {
        SendNavigationSubmitEvent(button);
    }

    public static void SendNavigationSubmitEvent(this VisualElement visualElement)
    {
        visualElement.Focus();
        using NavigationSubmitEvent evt = new NavigationSubmitEvent()
        {
            target = visualElement
        };
        visualElement.SendEvent(evt);
    }

    public static void SendPointerDownEvent(this VisualElement visualElement)
    {
        using PointerDownEvent evt = new PointerDownEvent()
        {
            target = visualElement
        };
        visualElement.SendEvent(evt);
    }
}
