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
}
