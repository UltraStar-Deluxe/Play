using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class FocusableNavigatorControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private EventSystem eventSystem;

    public VisualElement FocusedVisualElement => uiDocument.rootVisualElement.focusController.focusedElement as VisualElement;
    private VisualElement lastFocusedVisualElement;

    private void Start()
    {
        eventSystem.sendNavigationEvents = false;
    }

    private void Update()
    {
        // There should always be a focused element
        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement == null
            && lastFocusedVisualElement != null
            && IsFocusableNow(lastFocusedVisualElement))
        {
            lastFocusedVisualElement.Focus();
        }
        lastFocusedVisualElement = focusedVisualElement;
    }

    public void OnNavigate(Vector2 navigationDirection)
    {
        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement == null)
        {
            return;
        }

        VisualElement focusableNavigatorRootVisualElement = GetFocusableNavigatorRootVisualElement(focusedVisualElement);
        if (focusableNavigatorRootVisualElement == null)
        {
            return;
        }
        Vector2 targetPosition = focusedVisualElement.worldBound.center;

        List<VisualElement> focusableVisualElements = GetFocusableVisualElementsInDescendants(focusableNavigatorRootVisualElement);
        focusableVisualElements = focusableVisualElements
            .Where(it => it != focusedVisualElement)
            .ToList();
        List<VisualElement> visualElementsInDirection = GetVisualElementsInDirection(
            targetPosition,
            navigationDirection,
            focusableVisualElements);

        VisualElement nearestVisualElement = visualElementsInDirection.FindMinElement(visualElement => Vector2.Distance(visualElement.worldBound.center, targetPosition));
        if (nearestVisualElement != null)
        {
            nearestVisualElement.Focus();
            nearestVisualElement.ScrollToSelf();
        }
    }

    private List<VisualElement> GetFocusableVisualElementsInDescendants(VisualElement rootVisualElement)
    {
        List<VisualElement> descendants = rootVisualElement.Query<VisualElement>()
            .Where(descendant => descendant.tabIndex > 0
                || descendant
                    is Button
                    or Toggle
                    or DropdownField
                    or TextField
                    or MinMaxSlider
                    or RadioButton)
            .Where(descendant => IsFocusableNow(descendant))
            .ToList();
        return descendants;
    }

    private VisualElement GetFocusableNavigatorRootVisualElement(VisualElement visualElement)
    {
        if (visualElement == null
            || visualElement.ClassListContains(R.UxmlClasses.focusableNavigatorRoot))
        {
            return visualElement;
        }

        return GetFocusableNavigatorRootVisualElement(visualElement.parent);
    }

    private List<VisualElement> GetVisualElementsInDirection(Vector2 startPoint, Vector2 direction, List<VisualElement> visualElements)
    {
        return visualElements.Where(visualElement =>
            {
                Vector2 currentPoint = visualElement.worldBound.center;
                Vector2 towardsCurrentPointDirection = currentPoint - startPoint;
                // Y-Axis must be inverted
                float angle = Vector2.Angle(direction, new Vector2(towardsCurrentPointDirection.x, -towardsCurrentPointDirection.y));
                return angle is < 90 or > 270;
            })
            .ToList();
    }

    private bool IsFocusableNow(VisualElement visualElement)
    {
        return visualElement.IsVisibleByDisplay()
               && !float.IsNaN(visualElement.worldBound.center.x)
               && !float.IsNaN(visualElement.worldBound.center.y);
    }

    public void OnSubmit()
    {
        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement != null)
        {
            focusedVisualElement.SendEvent(new NavigationSubmitEvent());
        }
    }
}
