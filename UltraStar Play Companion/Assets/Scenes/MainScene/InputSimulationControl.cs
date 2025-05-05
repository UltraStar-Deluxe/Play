using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputSimulationControl : INeedInjection, IInjectionFinishedListener
{
    private const float ClickTimeThresholdInSeconds = 0.3f;
    private const float DoubleClickTimeThresholdInSeconds = ClickTimeThresholdInSeconds * 2;
    private const float MousePadAreaTotalPointerDeltaThresholdInPercent = 0.02f;

    [Inject]
    private Settings settings;

    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject]
    private ApplicationManager applicationManager;

    [Inject(UxmlName = R.UxmlNames.simulateLeftButton)]
    private Button simulateLeftButton;

    [Inject(UxmlName = R.UxmlNames.simulateRightButton)]
    private Button simulateRightButton;

    [Inject(UxmlName = R.UxmlNames.simulateUpButton)]
    private Button simulateUpButton;

    [Inject(UxmlName = R.UxmlNames.simulateDownButton)]
    private Button simulateDownButton;

    [Inject(UxmlName = R.UxmlNames.simulateEnterButton)]
    private Button simulateEnterButton;

    [Inject(UxmlName = R.UxmlNames.simulateEscapeButton)]
    private Button simulateEscapeButton;

    [Inject(UxmlName = R.UxmlNames.simulateSpaceButton)]
    private Button simulateSpaceButton;

    [Inject(UxmlName = R.UxmlNames.simulateVolumeUpButton)]
    private Button simulateVolumeUpButton;

    [Inject(UxmlName = R.UxmlNames.simulateVolumeDownButton)]
    private Button simulateVolumeDownButton;

    [Inject(UxmlName = R.UxmlNames.simulateLeftMouseButton)]
    private Button simulateLeftMouseButton;

    [Inject(UxmlName = R.UxmlNames.simulateRightMouseButton)]
    private Button simulateRightMouseButton;

    [Inject(UxmlName = R.UxmlNames.simulateMiddleMouseButton)]
    private Button simulateMiddleMouseButton;

    [Inject(UxmlName = R.UxmlNames.mousePadArea)]
    private VisualElement mousePadArea;

    [Inject(UxmlName = R.UxmlNames.scrollWheelArea)]
    private VisualElement scrollWheelArea;

    [Inject(UxmlName = R.UxmlNames.showKeyboardSimulationButton)]
    private Button showKeyboardSimulationButton;

    [Inject(UxmlName = R.UxmlNames.showMouseSimulationButton)]
    private Button showMouseSimulationButton;

    [Inject(UxmlName = R.UxmlNames.keyboardSimulationContainer)]
    private VisualElement keyboardSimulationContainer;

    [Inject(UxmlName = R.UxmlNames.mouseSimulationContainer)]
    private VisualElement mouseSimulationContainer;

    private Vector3 lastScrollWheelAreaPos;

    private bool isAllFingersUp;

    private bool isPointerDownOnMousePadArea;

    // TODO: Create DragDetectionControl that identifies drag start and end vs. single click vs. double click
    private Vector3 mousePadAreaStartPos;
    private Vector3 lastMousePadAreaPos;
    private bool isMousePadAreaTotalPointerDeltaAboveThreshold;

    private int mousePadAreaPointerDownEventClickCount;
    // Workaround for clickCount always 1 on Android: calculate manually
    private float timeInSecondsSinceLastPointerDownOnMousePadArea;

    private bool awaitingDragEnd;

    private readonly TabGroupControl tabGroupControl = new();

    private readonly Subject<CustomPointerMoveEvent> pointerMoveOnMousePadAreaEventStream = new();
    private readonly Subject<CustomPointerMoveEvent> pointerMoveOnScrollWheelAreaEventStream = new();

    public void OnInjectionFinished()
    {
        tabGroupControl.AddTabGroupButton(showKeyboardSimulationButton, keyboardSimulationContainer);
        tabGroupControl.AddTabGroupButton(showMouseSimulationButton, mouseSimulationContainer);
        tabGroupControl.ShowContainer(keyboardSimulationContainer);

        RegisterCallbackToSendSimulationInputRequest(simulateLeftButton, "leftArrowKey");
        RegisterCallbackToSendSimulationInputRequest(simulateRightButton, "rightArrowKey");
        RegisterCallbackToSendSimulationInputRequest(simulateUpButton, "upArrowKey");
        RegisterCallbackToSendSimulationInputRequest(simulateDownButton, "downArrowKey");
        RegisterCallbackToSendSimulationInputRequest(simulateEnterButton, "enterKey");
        RegisterCallbackToSendSimulationInputRequest(simulateEscapeButton, "escapeKey");
        RegisterCallbackToSendSimulationInputRequest(simulateSpaceButton, "spaceKey");
        RegisterCallbackToSendSimulationInputRequest(simulateVolumeUpButton, "volumeUpKey");
        RegisterCallbackToSendSimulationInputRequest(simulateVolumeDownButton, "volumeDownKey");
        RegisterCallbackToSendSimulationInputRequest(simulateLeftMouseButton, "leftMouseButton");
        RegisterCallbackToSendSimulationInputRequest(simulateRightMouseButton, "rightMouseButton");
        RegisterCallbackToSendSimulationInputRequest(simulateMiddleMouseButton, "middleMouseButton");

        mousePadArea.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterMousePadArea(evt), TrickleDown.TrickleDown);
        mousePadArea.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveMousePadArea(evt), TrickleDown.TrickleDown);
        mousePadArea.RegisterCallback<PointerMoveEvent>(evt => OnPointerMoveOnMousePadArea(evt), TrickleDown.TrickleDown);
        mousePadArea.RegisterCallback<PointerDownEvent>(evt => OnPointerDownOnMousePadArea(evt), TrickleDown.TrickleDown);
        mousePadArea.GetRootVisualElement().RegisterCallback<PointerUpEvent>(evt => OnPointerUpOnRootVisualElement(evt), TrickleDown.TrickleDown);

        scrollWheelArea.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerMoveEvent>(evt => OnPointerMoveOnScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerDownEvent>(evt => OnPointerDownOnScrollWheelArea(evt));

        // The pointer move event is fired very often.
        // To send a manageable amount of events, buffer them.
        pointerMoveOnMousePadAreaEventStream
            .Buffer(TimeSpan.FromMilliseconds(50))
            .CatchIgnore((Exception ex) => Debug.LogException(ex))
            .Subscribe(events =>
            {
                OnPointerMoveOnMousePadAreaBufferedEvents(events);
            });

        pointerMoveOnScrollWheelAreaEventStream
            .Buffer(TimeSpan.FromMilliseconds(50))
            .CatchIgnore((Exception ex) => Debug.LogException(ex))
            .Subscribe(events =>
            {
                OnPointerMoveOnScrollWheelAreaBufferedEvents(events);
            });

        applicationManager.FingerUpEventStream.Subscribe(_ =>
        {
            // The event seems to be fired before the count is decreased.
            // Thus, check for count smaller or equal than 1.
            if (Touch.activeTouches.Count <= 1
                || Touch.activeFingers.Count <= 1)
            {
                isAllFingersUp = true;
            }
        });
    }

    // Method is public to access it from test
    public async Awaitable SendSimulateLeftMouseButtonClickRequestAsync()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        Debug.Log("simulating left mouse button single click");
        await SendSimulateInputRequestAsync("leftMouseButton");
    }

    private async void SendSimulateLeftMouseButtonClickRequest()
    {
        await SendSimulateLeftMouseButtonClickRequestAsync();
    }

    private async void SendSimulateLeftMouseButtonDoubleClickRequest()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        Debug.Log("simulating left mouse button double click");
        await SendSimulateInputRequestAsync("leftMouseButton");
        await SendSimulateInputRequestAsync("leftMouseButton");
    }

    private async void SendSimulateDragStartRequest()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        awaitingDragEnd = true;
        Debug.Log("simulating drag start");
        await SendSimulateInputRequestAsync("dragStart");
    }

    private async void SendSimulateDragEndRequest()
    {
        if (!awaitingDragEnd)
        {
            return;
        }

        awaitingDragEnd = false;
        Debug.Log("simulating drag end");
        await SendSimulateInputRequestAsync("dragEnd");
    }

    private void OnPointerUpOnRootVisualElement(PointerUpEvent evt)
    {
        Log.Debug(() => $"OnPointerUpOnRootVisualElement: {evt.deltaPosition}");
        isPointerDownOnMousePadArea = false;

        if (awaitingDragEnd)
        {
            SendSimulateDragEndRequest();
        }
    }

    private async void OnPointerDownOnMousePadArea(PointerDownEvent evt)
    {
        Log.Debug(() => "OnPointerDownOnMousePadArea");
        UpdateClickCountOnPointerDownOnMousePadArea();

        isAllFingersUp = false;
        isMousePadAreaTotalPointerDeltaAboveThreshold = false;
        isPointerDownOnMousePadArea = true;
        mousePadAreaStartPos = evt.localPosition;
        lastMousePadAreaPos = evt.localPosition;

        if (mousePadAreaPointerDownEventClickCount == 1)
        {
            // Check for single click, i.e. released all fingers after single click
            await Awaitable.WaitForSecondsAsync(ClickTimeThresholdInSeconds);
            if (!isPointerDownOnMousePadArea
                && mousePadAreaPointerDownEventClickCount == 1
                && !isMousePadAreaTotalPointerDeltaAboveThreshold
                && !awaitingDragEnd)
            {
                SendSimulateLeftMouseButtonClickRequest();
            }
        }
        else if (mousePadAreaPointerDownEventClickCount == 2)
        {
            // Check for double click, i.e. released all fingers after double click
            await Awaitable.WaitForSecondsAsync(DoubleClickTimeThresholdInSeconds);
            if (!isPointerDownOnMousePadArea
                && mousePadAreaPointerDownEventClickCount == 2
                && !isMousePadAreaTotalPointerDeltaAboveThreshold
                && !awaitingDragEnd)
            {
                SendSimulateLeftMouseButtonDoubleClickRequest();
            }
        }
    }

    private void UpdateClickCountOnPointerDownOnMousePadArea()
    {
        if (TimeUtils.IsDurationAboveThresholdInSeconds(timeInSecondsSinceLastPointerDownOnMousePadArea, 0.5f)
            || isMousePadAreaTotalPointerDeltaAboveThreshold)
        {
            mousePadAreaPointerDownEventClickCount = 0;
        }
        timeInSecondsSinceLastPointerDownOnMousePadArea = Time.time;
        mousePadAreaPointerDownEventClickCount++;
    }

    private void OnPointerEnterMousePadArea(PointerEnterEvent evt)
    {
        Log.Debug(() => "OnPointerEnterMousePadArea");
        mousePadAreaStartPos = evt.localPosition;
        lastMousePadAreaPos = evt.localPosition;
    }

    private void OnPointerLeaveMousePadArea(PointerLeaveEvent evt)
    {
        Log.Debug(() => "OnPointerLeaveMousePadArea");
    }

    private void OnPointerEnterScrollWheelArea(PointerEnterEvent evt)
    {
        Log.Debug(() => "OnPointerEnterScrollWheelArea");
        lastScrollWheelAreaPos = evt.localPosition;
    }

    private void OnPointerLeaveScrollWheelArea(PointerLeaveEvent evt)
    {
        Log.Debug(() => "OnPointerLeaveScrollWheelArea");
    }

    private void OnPointerMoveOnScrollWheelArea(PointerMoveEvent evt)
    {
        pointerMoveOnScrollWheelAreaEventStream.OnNext(new CustomPointerMoveEvent()
        {
            localPosition = evt.localPosition,
            targetVisualElement = evt.target as VisualElement,
        });
    }

    private void OnPointerDownOnScrollWheelArea(PointerDownEvent evt)
    {
        Log.Debug(() => "OnPointerDownOnScrollWheelArea");
        lastScrollWheelAreaPos = evt.localPosition;
    }

    private void OnPointerMoveOnScrollWheelAreaBufferedEvents(IList<CustomPointerMoveEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        VisualElement targetVisualElement = events.Last().targetVisualElement;
        Vector3 localPosition = events.Last().localPosition;
        Log.Verbose(() => $"OnPointerMoveOnScrollWheelAreaBufferedEvents - events: {events.Count}, localPosition: {localPosition}");

        if (targetVisualElement != scrollWheelArea)
        {
            lastScrollWheelAreaPos = localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            // All fingers up => reset position
            isAllFingersUp = false;
            lastScrollWheelAreaPos = localPosition;
            return;
        }

        Vector3 pointerDelta = localPosition - lastScrollWheelAreaPos;
        if (Math.Abs(pointerDelta.y) > scrollWheelArea.contentRect.height / 10)
        {
            float deltaX = 0;
            float deltaY = Math.Sign(-pointerDelta.y);
            SendSimulateScrollWheelRequest(new Vector2(deltaX, deltaY));
            lastScrollWheelAreaPos = localPosition;
        }
    }

    private Vector3 GetAverageLocalPosition(IList<CustomPointerMoveEvent> events)
    {
        float averageLocalPositionX = events
            .Select(evt => evt.localPosition)
            .Average(localPosition => localPosition.x);
        float averageLocalPositionY = events
            .Select(evt => evt.localPosition)
            .Average(localPosition => localPosition.y);
        return new Vector3(averageLocalPositionX, averageLocalPositionY);
    }

    private void OnPointerMoveOnMousePadArea(PointerMoveEvent evt)
    {
        pointerMoveOnMousePadAreaEventStream.OnNext(new CustomPointerMoveEvent()
        {
            localPosition = evt.localPosition,
            targetVisualElement = evt.target as VisualElement,
        });
    }

    private void OnPointerMoveOnMousePadAreaBufferedEvents(IList<CustomPointerMoveEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        VisualElement targetVisualElement = events.Last().targetVisualElement;
        Vector3 localPosition = events.Last().localPosition;
        Log.Verbose(() => $"OnPointerMoveOnMousePadAreaBufferedEvents - events: {events.Count}, localPosition: {localPosition}");

        if (targetVisualElement != mousePadArea
            || !isPointerDownOnMousePadArea)
        {
            Log.Verbose(() => $"OnPointerMoveOnMousePadAreaBufferedEvents - aborting because pointer not down on mouse pad area");

            mousePadAreaStartPos = localPosition;
            lastMousePadAreaPos = localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            Log.Verbose(() => $"OnPointerMoveOnMousePadAreaBufferedEvents - aborting because all fingers up");

            // All fingers up => reset position
            isAllFingersUp = false;
            mousePadAreaStartPos = localPosition;
            lastMousePadAreaPos = localPosition;
            return;
        }

        float mousePadAreaMagnitude = mousePadArea.worldBound.size.magnitude;
        if (mousePadAreaMagnitude > 0)
        {
            Vector3 totalPointerDelta = localPosition - mousePadAreaStartPos;
            float magnitudeInPercent = totalPointerDelta.magnitude / mousePadAreaMagnitude;
            Log.Verbose(() => $"pointerDeltaMagnitudeInPercent: {magnitudeInPercent}");
            if (magnitudeInPercent > MousePadAreaTotalPointerDeltaThresholdInPercent)
            {
                isMousePadAreaTotalPointerDeltaAboveThreshold = true;
            }
        }

        // Check for drag start, i.e. hold down after double click
        if (isPointerDownOnMousePadArea
            && mousePadAreaPointerDownEventClickCount == 2
            && isMousePadAreaTotalPointerDeltaAboveThreshold
            && !awaitingDragEnd)
        {
            Log.Verbose(() => $"OnPointerMoveOnMousePadAreaBufferedEvents - aborting because sending drag start event instead");
            SendSimulateDragStartRequest();
            return;
        }

        Vector3 pointerDelta = (localPosition - lastMousePadAreaPos) * settings.MousePadSensitivity;
        SendSimulateMouseDeltaRequest(new Vector2(pointerDelta.x, -pointerDelta.y));
        lastMousePadAreaPos = localPosition;
    }

    private async Awaitable SendSimulateInputRequestAsync(string inputControl)
    {
        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.Input
            .ReplaceOrThrow("{inputControl}", inputControl));
    }

    private async void SendSimulateScrollWheelRequest(Vector2 scrollDelta)
    {
        if (scrollDelta == Vector2.zero)
        {
            return;
        }

        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.InputScrollWheel
            .ReplaceOrThrow("{deltaX}", scrollDelta.x.ToString(CultureInfo.InvariantCulture))
            .ReplaceOrThrow("{deltaY}", scrollDelta.y.ToString(CultureInfo.InvariantCulture)));
    }

    private async void SendSimulateMouseDeltaRequest(Vector2 mouseDelta)
    {
        if (mouseDelta == Vector2.zero)
        {
            return;
        }

        if (Application.isEditor)
        {
            Log.Verbose(() => "Not sending input simulation request for mouse delta because the app is running in the Unity editor.");
            return;
        }

        await mainGameHttpClient.PostRequestAsync(RestApiEndpointPaths.InputMouseDelta
            .ReplaceOrThrow("{deltaX}", mouseDelta.x.ToStringInvariantCulture())
            .ReplaceOrThrow("{deltaY}", mouseDelta.y.ToStringInvariantCulture()));
    }

    private void RegisterCallbackToSendSimulationInputRequest(Button uiButton, string keyboardButton)
    {
        uiButton.RegisterCallbackButtonTriggered(async _ => await SendSimulateInputRequestAsync(keyboardButton));
    }

    private class CustomPointerMoveEvent
    {
        public Vector3 localPosition;
        public VisualElement targetVisualElement;
    }
}
