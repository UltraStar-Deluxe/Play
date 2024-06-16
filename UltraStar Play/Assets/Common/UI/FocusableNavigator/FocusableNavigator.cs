using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using Serilog.Events;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Changes focus between focusable VisualElements on navigation InputAction
 * (e.g. arrow keys or controller stick).
 * And sends submit event to focused VisualElement on corresponding InputAction.
 */
public class FocusableNavigator : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    protected UIDocument uiDocument;

    [Inject(Optional = true)]
    protected EventSystem eventSystem;

    [Inject]
    protected Settings settings;

    public bool focusLastElementIfNothingFocused;

    public LogEventLevel logLevel = LogEventLevel.Debug;

    public VisualElement FocusedVisualElement => uiDocument != null
        ? uiDocument.rootVisualElement?.focusController?.focusedElement as VisualElement
        : null;
    private bool triedToFocusLastVisualElement;
    protected VisualElement lastFocusedVisualElement;
    protected VisualElement lastFocusableNavigatorRootVisualElement;

    protected readonly Subject<NoNavigationTargetFoundEvent> noNavigationTargetFoundEventStream = new();
    public IObservable<NoNavigationTargetFoundEvent> NoNavigationTargetFoundEventStream => noNavigationTargetFoundEventStream;

    protected readonly Subject<NoSubmitTargetFoundEvent> noSubmitTargetFoundEventStream = new();
    public IObservable<NoSubmitTargetFoundEvent> NoSubmitTargetFoundEventStream => noSubmitTargetFoundEventStream;

    private readonly List<CustomNavigationTarget> customNavigationTargets = new();

    public Func<NoNavigationTargetFoundEvent, bool> NoNavigationTargetFoundInListViewCallback { get; set; }
    public Func<NavigationParameters, bool> BeforeNavigationInListViewCallback { get; set; }

    public virtual void OnInjectionFinished()
    {
        if (!PlatformUtils.IsAndroid
            || (eventSystem != null
                && settings.EnableEventSystemOnAndroid))
        {
            eventSystem.sendNavigationEvents = false;
        }

        noNavigationTargetFoundEventStream.Subscribe(evt =>
        {
            Log.WithLevel(logLevel, () => $"No navigation target found: {evt}");
        });
    }

    protected virtual void Update()
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (uiDocument == null)
        {
            // Injection failed
            return;
        }

        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement == null
            && focusLastElementIfNothingFocused)
        {
            TryFocusLastFocusedVisualElement();
        }
        else if (focusedVisualElement != null
                 && lastFocusedVisualElement != focusedVisualElement)
        {
            lastFocusedVisualElement = focusedVisualElement;
            lastFocusableNavigatorRootVisualElement = GetFocusableNavigatorRootVisualElement(lastFocusedVisualElement);
        }
    }

    protected void TryFocusLastFocusedVisualElement()
    {
        if (triedToFocusLastVisualElement)
        {
            return;
        }

        if (lastFocusedVisualElement != null)
        {
            if (IsFocusableNow(lastFocusedVisualElement))
            {
                DoFocusVisualElement(lastFocusedVisualElement,
                    $"Moving focus to last focused VisualElement: {lastFocusedVisualElement}");
            }
            else
            {
                // Try select other element in the hierarchy of the last focused element
                TryFocusElementInDescendants(lastFocusableNavigatorRootVisualElement,
                    descendant => descendant != lastFocusedVisualElement);
            }
        }
        triedToFocusLastVisualElement = true;
    }

    private void TryFocusElementInDescendants(VisualElement visualElement, Func<VisualElement, bool> filter = null)
    {
        if (visualElement == null)
        {
            return;
        }

        List<VisualElement> focusableElements = GetFocusableVisualElementsInDescendants(visualElement, filter);
        if (focusableElements.IsNullOrEmpty())
        {
            return;
        }

        VisualElement focusableElement = focusableElements.FirstOrDefault();
        if (focusableElement != null)
        {
            focusableElement.Focus();
        }
    }

    public virtual void OnBack()
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (PlatformUtils.IsAndroid
            && !settings.EnableEventSystemOnAndroid)
        {
            return;
        }

        bool cancelNotifyForThisFrame = false;
        if (VisualElementUtils.IsDropdownListFocused(uiDocument.rootVisualElement.focusController))
        {
            FocusedVisualElement.SendEvent(NavigationCancelEvent.GetPooled());
            cancelNotifyForThisFrame = true;
        }
        else if (!ContextMenuPopupControl.OpenContextMenuPopups.IsNullOrEmpty())
        {
            ContextMenuPopupControl.OpenContextMenuPopups
                .ToList()
                .ForEach(it => it.CloseContextMenu());
            cancelNotifyForThisFrame = true;
        }

        if (cancelNotifyForThisFrame)
        {
            InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
        }
    }

    public virtual void OnSubmit()
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (PlatformUtils.IsAndroid
            && !settings.EnableEventSystemOnAndroid)
        {
            return;
        }

        if (VisualElementUtils.IsDropdownListFocused(uiDocument.rootVisualElement.focusController, out VisualElement unityBaseDropdown))
        {
            // TODO: Submit does not work ( https://forum.unity.com/threads/navigation-and-dropdownfield.1195423/ )
            FocusedVisualElement.SendEvent(new NavigationSubmitEvent() { target = FocusedVisualElement });
            return;
        }

        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement != null)
        {
            using NavigationSubmitEvent evt = new NavigationSubmitEvent()
            {
                target = focusedVisualElement

            };
            focusedVisualElement.SendEvent(evt);
        }
        else
        {
            noSubmitTargetFoundEventStream.OnNext(new NoSubmitTargetFoundEvent());
        }
    }

    public virtual void OnNavigate(Vector2 navigationDirection, InputAction inputAction, InputControl inputControl)
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (PlatformUtils.IsAndroid
            && !settings.EnableEventSystemOnAndroid)
        {
            return;
        }

        VisualElement focusedVisualElement = FocusedVisualElement;
        if (focusedVisualElement == null)
        {
            Debug.LogWarning($"FocusableNavigator.OnNavigate: No focused VisualElement found. lastFocusedVisualElement: {lastFocusedVisualElement?.name}, lastFocusableNavigatorRootVisualElement: {lastFocusableNavigatorRootVisualElement?.name}");
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
                // Unity only sends navigation events to TextField if they come from a keyboard.
                // These events are ignored here to move the cursor or select text in the TextField.
                if (inputControl != null
                    && inputControl.device is Keyboard)
                {
                    return;
                }
            }
        }

        // Use custom navigation target if any
        if (TryNavigateToCustomNavigationTarget(focusedVisualElement, navigationDirection))
        {
            return;
        }

        if (VisualElementUtils.IsDropdownListFocused(uiDocument.rootVisualElement.focusController))
        {
            NavigateDropdownList(focusedVisualElement, navigationDirection);
            return;
        }

        if (focusedVisualElement is ListView listView
            && TryNavigateListView(listView, navigationDirection))
        {
            return;
        }

        if (focusedVisualElement is ListViewH listViewH
            && TryNavigateListView(listViewH, navigationDirection))
        {
            return;
        }

        ListViewH parentListViewH = focusedVisualElement.GetFirstAncestorOfType<ListViewH>();
        if ((parentListViewH != null)
            && TryNavigateListView(parentListViewH, navigationDirection))
        {
            return;
        }

        VisualElement parentVisualElement = focusedVisualElement.GetParent(parent => parent.ClassListContains(R.UssClasses.focusableNavigatorPriorityParent));
        if (parentVisualElement != null
            && TryNavigateInVisualElement(parentVisualElement, focusedVisualElement, navigationDirection))
        {
            return;
        }

        ScrollView parentScrollView = focusedVisualElement.GetFirstAncestorOfType<ScrollView>();
        if (parentScrollView != null
            && TryNavigateInVisualElement(parentScrollView, focusedVisualElement, navigationDirection))
        {
            return;
        }

        NavigateToBestMatchingNavigationTarget(focusedVisualElement, navigationDirection);
    }

    private bool TryNavigateInVisualElement(
        VisualElement visualElement,
        VisualElement focusedVisualElement,
        Vector2 navigationDirection)
    {
        // Try to navigate within the ScrollView
        List<VisualElement> focusableVisualElements = GetFocusableVisualElementsInDescendants(visualElement);
        return TryNavigateToBestMatchingNavigationTarget(focusedVisualElement, navigationDirection, focusableVisualElements);
    }

    private bool TryNavigateListView(
        ListView listView,
        Vector2 navigationDirection)
    {
        if (listView.itemsSource.Count == 0)
        {
            return false;
        }

        int selectedIndex = listView.selectedIndex;
        if (navigationDirection.y > 0
            && selectedIndex > 0)
        {
            Log.WithLevel(logLevel, () => "Select previous item in ListView");
            listView.SetSelectionAndScrollTo(selectedIndex - 1);
            TryFocusSelectedListViewItem(listView);
            return true;
        }
        else if (navigationDirection.y < 0
                 && selectedIndex < listView.itemsSource.Count - 1)
        {
            Log.WithLevel(logLevel, () => "Select next item in ListView");
            listView.SetSelectionAndScrollTo(selectedIndex + 1);
            TryFocusSelectedListViewItem(listView);
            return true;
        }

        if (NoNavigationTargetFoundInListViewCallback != null)
        {
            bool isHandled = NoNavigationTargetFoundInListViewCallback.Invoke(new NoNavigationTargetFoundEvent()
            {
                NavigationDirection = navigationDirection,
                FocusedVisualElement = listView,
                FocusableNavigatorRootVisualElement = GetFocusableNavigatorRootVisualElement(),
            });
            return isHandled;
        }

        return false;
    }

    private bool TryNavigateListView(
        ListViewH listView,
        Vector2 navigationDirection)
    {
        if (listView.itemsSource.Count == 0)
        {
            return false;
        }

        if (BeforeNavigationInListViewCallback != null)
        {
            bool isHandled = BeforeNavigationInListViewCallback(new NavigationParameters()
            {
                focusedVisualElement = listView,
                navigationDirection = navigationDirection,
            });
            if (isHandled)
            {
                return true;
            }
        }

        int selectedIndex = listView.selectedIndex;
        if (navigationDirection.x < 0
            && selectedIndex > 0)
        {
            Log.WithLevel(logLevel, () => "Select previous item in ListView");
            listView.SetSelectionAndScrollTo(selectedIndex - 1);
            TryFocusSelectedListViewItem(listView);
            return true;
        }
        else if (navigationDirection.x > 0
                 && selectedIndex < listView.itemsSource.Count - 1)
        {
            Log.WithLevel(logLevel, () => "Select next item in ListView");
            listView.SetSelectionAndScrollTo(selectedIndex + 1);
            TryFocusSelectedListViewItem(listView);
            return true;
        }

        if (NoNavigationTargetFoundInListViewCallback != null)
        {
            bool isHandled = NoNavigationTargetFoundInListViewCallback.Invoke(new NoNavigationTargetFoundEvent()
            {
                NavigationDirection = navigationDirection,
                FocusedVisualElement = listView,
                FocusableNavigatorRootVisualElement = GetFocusableNavigatorRootVisualElement(),
            });
            return isHandled;
        }

        return false;
    }

    private void TryFocusSelectedListViewItem(ListView listView)
    {
        VisualElement selectedVisualElement = listView.GetSelectedVisualElement();
        if (selectedVisualElement != null)
        {
            List<VisualElement> focusableVisualElements = GetFocusableVisualElementsInDescendants(selectedVisualElement);
            if (focusableVisualElements.IsNullOrEmpty())
            {
                return;
            }

            VisualElement firstFocusableVisualElement = focusableVisualElements[0];
            Log.WithLevel(logLevel, () => $"Moving focus to first focusable VisualElement in selected ListView item: {firstFocusableVisualElement}");
            firstFocusableVisualElement.Focus();
        }
    }

    private void TryFocusSelectedListViewItem(ListViewH listView)
    {
        VisualElement selectedVisualElement = listView.GetSelectedVisualElement();
        if (selectedVisualElement != null)
        {
            List<VisualElement> focusableVisualElements = GetFocusableVisualElementsInDescendants(selectedVisualElement);
            if (focusableVisualElements.IsNullOrEmpty())
            {
                return;
            }

            VisualElement firstFocusableVisualElement = focusableVisualElements[0];
            Log.WithLevel(logLevel, () => $"Moving focus to first focusable VisualElement in selected ListView item: {firstFocusableVisualElement}");
            firstFocusableVisualElement.Focus();
        }
    }

    private void NavigateDropdownList(
        VisualElement focusedVisualElement,
        Vector2 navigationDirection)
    {
        Log.WithLevel(logLevel, () => "NavigateDropdownList");

        ListView dropdownListView = focusedVisualElement.Q<ListView>(null, "unity-base-dropdown__container-inner");
        TryNavigateListView(dropdownListView, navigationDirection);
    }

    private void NavigateToBestMatchingNavigationTarget(VisualElement focusedVisualElement, Vector2 navigationDirection)
    {
        // Find eligible elements for navigation, i.e., all descendants of the current focusableNavigatorRootVisualElement.
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

        List<VisualElement> focusableVisualElements = GetFocusableVisualElementsInDescendants(focusableNavigatorRootVisualElement);
        focusableVisualElements = focusableVisualElements
            .Where(it => it != focusedVisualElement)
            .ToList();

        if (!TryNavigateToBestMatchingNavigationTarget(focusedVisualElement, navigationDirection, focusableVisualElements))
        {
            noNavigationTargetFoundEventStream.OnNext(new NoNavigationTargetFoundEvent
            {
                NavigationDirection = navigationDirection,
                FocusableNavigatorRootVisualElement = focusableNavigatorRootVisualElement,
                FocusedVisualElement = focusedVisualElement,
            });
        }
    }

    private bool TryNavigateToBestMatchingNavigationTarget(
        VisualElement focusedVisualElement,
        Vector2 navigationDirection,
        List<VisualElement> focusableVisualElements)
    {
        // Only consider the VisualElements that are in the navigation direction
        Rect startRect = focusedVisualElement.worldBound;
        List<VisualElement> visualElementsInDirection = GetVisualElementsInDirection(
            startRect,
            navigationDirection,
            focusableVisualElements);

        // Choose the nearest VisualElement
        VisualElement nearestVisualElement = visualElementsInDirection.FindMinElement(visualElement => GetVisualElementDistance(visualElement, focusedVisualElement));
        if (nearestVisualElement != null)
        {
            DoFocusVisualElement(nearestVisualElement,
                $"Moving focus to VisualElement with distance {GetVisualElementDistance(nearestVisualElement, focusedVisualElement)}: {nearestVisualElement}");
            return true;
        }
        else
        {
            return false;
        }
    }

    protected void DoFocusVisualElement(VisualElement visualElement, string logMessage)
    {
        if (!logMessage.IsNullOrEmpty())
        {
            Log.WithLevel(logLevel, () => logMessage);
        }

        if (visualElement == null)
        {
            Debug.LogError("Attempt to focus null VisualElement");
            return;
        }

        if (visualElement is ListView focusedListView)
        {
            TryFocusSelectedListViewItem(focusedListView);
        }

        if (visualElement is ListViewH focusedListViewH)
        {
            TryFocusSelectedListViewItem(focusedListViewH);
        }

        ListView parentListView = visualElement.GetFirstAncestorOfType<ListView>();
        ListViewH parentListViewH = visualElement.GetFirstAncestorOfType<ListViewH>();
        if (parentListView != null)
        {
            parentListView.Focus();
            parentListView.ScrollToSelf();

            TryFocusSelectedListViewItem(parentListView);
        }
        else if (parentListViewH != null)
        {
            parentListViewH.Focus();
            parentListViewH.ScrollToSelf();

            TryFocusSelectedListViewItem(parentListViewH);
        }
        else
        {
            visualElement.Focus();
            visualElement.ScrollToSelf();
        }

        triedToFocusLastVisualElement = false;
    }

    private bool TryNavigateToCustomNavigationTarget(VisualElement focusedVisualElement, Vector2 navigationDirection)
    {
        CustomNavigationTarget customNavigationTarget = customNavigationTargets.FirstOrDefault(customNavigationTarget =>
            customNavigationTarget.Matches(focusedVisualElement, navigationDirection));
        if (customNavigationTarget != null
            && IsFocusableNow(customNavigationTarget.TargetVisualElement))
        {
            DoFocusVisualElement(customNavigationTarget.TargetVisualElement,
                    $"Moving focus to VisualElement from custom navigation target (start: {customNavigationTarget.StartVisualElement.name}, direction: {navigationDirection}, target: {customNavigationTarget.TargetVisualElement.name}");
            return true;
        }

        return false;
    }

    public void AddCustomNavigationTarget(
        VisualElement startVisualElement,
        Vector2 navigationDirection,
        VisualElement targetVisualElement,
        bool alsoAddOppositeDirection = false)
    {
        CustomNavigationTarget customNavigationTarget = new(startVisualElement, navigationDirection, targetVisualElement);
        customNavigationTargets.Add(customNavigationTarget);

        if (alsoAddOppositeDirection)
        {
            AddCustomNavigationTarget(targetVisualElement, -navigationDirection, startVisualElement, false);
        }
    }

    public void RemoveCustomNavigationTarget(
        VisualElement startVisualElement,
        Vector2 navigationDirection,
        bool alsoAddOppositeDirection = false)
    {
        CustomNavigationTarget customNavigationTarget = customNavigationTargets
            .FirstOrDefault(it => it.StartVisualElement == startVisualElement && it.NavigationDirection == navigationDirection);
        customNavigationTargets.Remove(customNavigationTarget);

        if (alsoAddOppositeDirection)
        {
            RemoveCustomNavigationTarget(startVisualElement, -navigationDirection, false);
        }
    }

    private static float GetVisualElementDistance(VisualElement visualElementA, VisualElement visualElementB)
    {
        float RectangleDistance(Rect rectA, Rect rectB)
        {
            // https://gamedev.stackexchange.com/questions/154036/efficient-minimum-distance-between-two-axis-aligned-squares
            Rect rectOuter = new Rect(
                Mathf.Min(rectA.xMin, rectB.xMin),
                Mathf.Min(rectA.yMin, rectB.yMin),
                Mathf.Max(rectA.xMax, rectB.xMax) - Mathf.Min(rectA.xMin, rectB.xMin),
                Mathf.Max(rectA.yMax, rectB.yMax) - Mathf.Min(rectA.yMin, rectB.yMin));
            float innerWidth = Mathf.Max(0, rectOuter.width - rectA.width - rectB.width);
            float innerHeight = Mathf.Max(0, rectOuter.height - rectA.height - rectB.height);
            float minDistance = Mathf.Sqrt(innerWidth * innerWidth + innerHeight * innerHeight);
            return minDistance;
        }

        float distance = RectangleDistance(visualElementA.worldBound, visualElementB.worldBound);

        return distance;
    }

    private bool IsNavigatingAwayFromTextField(Vector2 navigationDirection, TextField focusedTextField)
    {
        if (focusedTextField.isReadOnly)
        {
            return true;
        }

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

    protected virtual List<VisualElement> GetFocusableVisualElementsInDescendants(VisualElement rootVisualElement, Func<VisualElement, bool> filter = null)
    {
        if (rootVisualElement == null)
        {
            return null;
        }

        List<VisualElement> descendants = rootVisualElement.Query<VisualElement>()
            .Where(descendant => filter == null || filter(descendant))
            .Where(descendant => descendant.tabIndex > 0
                || descendant
                    is Button
                    or Toggle
                    or SlideToggle
                    or DropdownField
                    or EnumField
                    or TextField
                    or IntegerField
                    or LongField
                    or FloatField
                    or DoubleField
                    or MinMaxSlider
                    or RadioButton
                    or ListView
                    or ListViewH)
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

        if (visualElement.ClassListContains(R.UssClasses.focusableNavigatorRoot))
        {
            return visualElement;
        }

        return GetFocusableNavigatorRootVisualElement(visualElement.parent);
    }

    protected List<VisualElement> GetVisualElementsInDirection(Rect startRect, Vector2 direction, List<VisualElement> visualElements)
    {
        bool IsInDirection(Rect rect)
        {
            // LEFT SIDE is xMin, RIGHT SIDE is xMax, TOP SIDE is yMin, BOTTOM SIDE is yMax
            if (direction.x < 0)
            {
                // Go left => RIGHT SIDE of target must be left of LEFT SIDE of start
                return rect.xMax <= startRect.xMin;
            }
            else if (direction.x > 0)
            {
                // Go right => LEFT SIDE of target must be right of RIGHT SIDE of start
                return rect.xMin >= startRect.xMax;
            }
            else if (direction.y <= 0)
            {
                // Go down => TOP SIDE of target must be below BOTTOM SIDE of start
                return rect.yMin >= startRect.yMax;
            }
            else if (direction.y > 0)
            {
                // Go up => BOTTOM SIDE of target must be above TOP SIDE of start
                return rect.yMax <= startRect.yMin;
            }

            return false;
        }

        Vector2 startRectCenter = startRect.center;
        return visualElements.Where(visualElement =>
            {
                Vector2 currentPoint = visualElement.worldBound.center;
                Vector2 towardsCurrentPointDirection = currentPoint - startRectCenter;
                // Y-Axis must be inverted
                float angle = Vector2.Angle(direction, new Vector2(towardsCurrentPointDirection.x, -towardsCurrentPointDirection.y));
                return angle is < 90 or > 270;
            })
            .Where(visualElement => IsInDirection(visualElement.worldBound))
            .ToList();
    }

    private bool IsFocusableNow(VisualElement visualElement)
    {
        return VisualElementUtils.IsFocusableNow(visualElement, uiDocument);
    }
}
