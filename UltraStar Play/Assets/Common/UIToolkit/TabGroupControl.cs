using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * Controls the visibility of a set of VisualElement containers, such that only one of them is visible at a time.
 * Thereby, each TabGroupButton toggles the visibility of one container.
 */
public class TabGroupControl
{
    private readonly Dictionary<Button, VisualElement> buttonToContainerMap = new Dictionary<Button, VisualElement>();

    public bool IsAnyContainerVisible => buttonToContainerMap.Values.AnyMatch(it => it.IsVisibleByDisplay());
    public bool AllowNoContainerVisible { get; set; }

    public void AddTabGroupButton(Button button, VisualElement controlledContainer)
    {
        buttonToContainerMap.Add(button, controlledContainer);
        button.RegisterCallbackButtonTriggered(() => OnButtonTriggered(button));
    }

    private void OnButtonTriggered(Button button)
    {
        if (!buttonToContainerMap.TryGetValue(button, out VisualElement controlledContainer))
        {
            return;
        }

        if (controlledContainer.IsVisibleByDisplay())
        {
            if (AllowNoContainerVisible)
            {
                HideAllContainers();
            }
        }
        else
        {
            ShowContainer(controlledContainer);
        }
    }

    public void HideAllContainers()
    {
        ShowContainer(null);
    }

    public void ShowContainer(VisualElement controlledContainer)
    {
        if (controlledContainer != null)
        {
            controlledContainer.ShowByDisplay();
        }
        // Hide others
        buttonToContainerMap.Values
            .Where(it => it != controlledContainer)
            .ForEach(it => it.HideByDisplay());
    }
}
