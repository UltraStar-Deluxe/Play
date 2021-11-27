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
}
