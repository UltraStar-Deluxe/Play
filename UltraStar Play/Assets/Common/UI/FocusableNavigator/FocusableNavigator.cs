using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Changes focus between focusable VisualElements on navigation InputAction
 * (e.g. arrow keys or controller stick).
 * And sends submit event to focused VisualElement on corresponding InputAction.
 */
public class FocusableNavigator : MonoBehaviour, INeedInjection
{
    [Inject]
    protected UIDocument uiDocument;

    [Inject]
    protected EventSystem eventSystem;

    public bool focusLastElementIfNothingFocused;

    public bool logFocusedVisualElements;

    public VisualElement FocusedVisualElement => uiDocument.rootVisualElement.focusController.focusedElement as VisualElement;
    protected VisualElement lastFocusedVisualElement;

    protected readonly Subject<NoNavigationTargetFoundEvent> noNavigationTargetFoundEventStream = new();
    public IObservable<NoNavigationTargetFoundEvent> NoNavigationTargetFoundEventStream => noNavigationTargetFoundEventStream;

    protected readonly Subject<NoSubmitTargetFoundEvent> noSubmitTargetFoundEventStream = new();
    public IObservable<NoSubmitTargetFoundEvent> NoSubmitTargetFoundEventStream => noSubmitTargetFoundEventStream;

    public virtual void Start()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        eventSystem.sendNavigationEvents = false;

        if (focusLastElementIfNothingFocused)
        {
            NoNavigationTargetFoundEventStream.Subscribe(evt =>
            {
                if (evt.FocusedVisualElement == null)
                {
                    FocusLastFocusedVisualElement();
                }
            });
        }
    }

    protected virtual void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (uiDocument == null)
        {
            // Injection failed
            return;
        }

        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement != null)
        {
            lastFocusedVisualElement = focusedVisualElement;
        }
    }

    public void FocusLastFocusedVisualElement()
    {
        if (lastFocusedVisualElement != null
            && IsFocusableNow(lastFocusedVisualElement))
        {
            if (logFocusedVisualElements)
            {
                Debug.Log("Moving focus to last focused VisualElement: " + lastFocusedVisualElement);
                lastFocusedVisualElement.Focus();
            }
        }
    }

    public virtual void OnSubmit()
    {
        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement != null)
        {
            focusedVisualElement.SendEvent(new NavigationSubmitEvent());
        }
        else
        {
            noSubmitTargetFoundEventStream.OnNext(new NoSubmitTargetFoundEvent());
        }
    }

    public virtual void OnNavigate(Vector2 navigationDirection)
    {
        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement == null)
        {
            noNavigationTargetFoundEventStream.OnNext(new NoNavigationTargetFoundEvent
            {
                NavigationDirection = navigationDirection,
                FocusableNavigatorRootVisualElement = null,
                FocusedVisualElement = null,
            });
            return;
        }

        // Keep focus on TextField if not navigating away from it
        if (focusedVisualElement is TextField focusedTextField)
        {
            if (!IsNavigatingAwayFromTextField(navigationDirection, focusedTextField))
            {
                return;
            }
        }

        // Find elements to include in navigation
        VisualElement focusableNavigatorRootVisualElement = GetFocusableNavigatorRootVisualElement(focusedVisualElement);
        if (focusableNavigatorRootVisualElement == null)
        {
            noNavigationTargetFoundEventStream.OnNext(new NoNavigationTargetFoundEvent
            {
                NavigationDirection = navigationDirection,
                FocusableNavigatorRootVisualElement = null,
                FocusedVisualElement = focusedVisualElement,
            });
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
            if (logFocusedVisualElements)
            {
                Debug.Log($"Moving focus to VisualElement: {nearestVisualElement}");
            }
            nearestVisualElement.Focus();
            nearestVisualElement.ScrollToSelf();
        }
        else
        {
            noNavigationTargetFoundEventStream.OnNext(new NoNavigationTargetFoundEvent
            {
                NavigationDirection = navigationDirection,
                FocusableNavigatorRootVisualElement = focusableNavigatorRootVisualElement,
                FocusedVisualElement = focusedVisualElement,
            });
        }
    }

    private bool IsNavigatingAwayFromTextField(Vector2 navigationDirection, TextField focusedTextField)
    {
        // Navigate away from TextField
        // when cursor was already at first or last position in text field
        // and still navigating towards same direction.
        bool isContinuedNavigationFromTextStart = focusedTextField.cursorIndex == 0
                                                  // Navigate left or up is continued navigation
                                                  && (navigationDirection.x < 0 || navigationDirection.y > 0);
        bool isContinuedNavigationFromTextEnd = focusedTextField.cursorIndex == focusedTextField.value.Length
                                                // Navigate right or down is continued navigation
                                                && (navigationDirection.x > 0 || navigationDirection.y < 0);
        bool isContinuedNavigation = isContinuedNavigationFromTextStart || isContinuedNavigationFromTextEnd;
        return isContinuedNavigation
               // There must not be any selection
               && focusedTextField.selectIndex == focusedTextField.cursorIndex;
    }

    protected virtual List<VisualElement> GetFocusableVisualElementsInDescendants(VisualElement rootVisualElement)
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

    protected VisualElement GetFocusableNavigatorRootVisualElement()
    {
        return GetFocusableNavigatorRootVisualElement(FocusedVisualElement);
    }

    protected virtual VisualElement GetFocusableNavigatorRootVisualElement(VisualElement visualElement)
    {
        if (visualElement == null)
        {
            return null;
        }

        if (visualElement.ClassListContains(R.UxmlClasses.focusableNavigatorRoot))
        {
            return visualElement;
        }

        return GetFocusableNavigatorRootVisualElement(visualElement.parent);
    }

    protected List<VisualElement> GetVisualElementsInDirection(Vector2 startPoint, Vector2 direction, List<VisualElement> visualElements)
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

    protected bool IsFocusableNow(VisualElement visualElement)
    {
        return visualElement != null
               && visualElement.IsVisibleByDisplay()
               && visualElement.GetAncestors().AllMatch(ancestor => ancestor.IsVisibleByDisplay())
               && !float.IsNaN(visualElement.worldBound.center.x)
               && !float.IsNaN(visualElement.worldBound.center.y)
               && !visualElement.ClassListContains(R.UxmlClasses.focusableNavigatorIgnore);
    }
}
