using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine.UIElements;

/**
 * Controls the visibility of a set of VisualElement containers, such that only one of them is visible at a time.
 * Thereby, each TabGroupButton toggles the visibility of one container.
 */
public class TabGroupControl
{
    private readonly Dictionary<Button, VisualElement> buttonToContainerMap = new();

    public bool IsAnyContainerVisible => buttonToContainerMap.Values.AnyMatch(it => it.IsVisibleByDisplay());
    public bool AllowNoContainerVisible { get; set; }

    private readonly Subject<VisualElement> containerBecameVisibleEventStream = new();
    public IObservable<VisualElement> ContainerBecameVisibleEventStream => containerBecameVisibleEventStream;

    public void AddTabGroupButton(Button button, VisualElement controlledContainer)
    {
        buttonToContainerMap.Add(button, controlledContainer);
        button.RegisterCallbackButtonTriggered(_ => OnButtonTriggered(button));
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
            buttonToContainerMap
                .Where(entry => entry.Value == controlledContainer)
                .ForEach(entry =>
                {
                    entry.Value.ShowByDisplay();
                    if (entry.Key is ToggleButton toggleButton)
                    {
                        toggleButton.SetActive(true);
                    }
                });
            
            containerBecameVisibleEventStream.OnNext(controlledContainer);
        }

        // Hide others
        buttonToContainerMap
            .Where(entry => entry.Value != controlledContainer)
            .ForEach(entry =>
            {
                entry.Value.HideByDisplay();
                if (entry.Key is ToggleButton toggleButton)
                {
                    toggleButton.SetActive(false);
                }
            });
    }
}
