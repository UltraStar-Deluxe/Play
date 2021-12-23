using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public static class VisualElementExtensions
{
    public static void RegisterCallbackButtonTriggered(this Button button, Action callback)
    {
        button.RegisterCallback<ClickEvent>(_ => callback());
        button.RegisterCallback<NavigationSubmitEvent>(_ => callback());
    }

    public static void AddToClassListIfNew(this VisualElement visualElement, params string[] newClasses)
    {
        HashSet<string> currentClasses = new HashSet<string>();
        visualElement.GetClasses().ForEach(currentClass => currentClasses.Add(currentClass));
        newClasses.ForEach(newClass =>
        {
            if (!currentClasses.Contains(newClass))
            {
                visualElement.AddToClassList(newClass);
            }
        });
    }

    public static bool IsVisibleByDisplay(this VisualElement visualElement)
    {
        return visualElement.style.display != DisplayStyle.None;
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
            }
        }

        visualElement.RegisterCallback<TEventType>(RunCallbackIfNotDoneYet, useTrickleDown);
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
}
