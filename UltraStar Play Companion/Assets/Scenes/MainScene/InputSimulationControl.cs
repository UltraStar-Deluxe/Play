using System;
using System.Globalization;
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

    private bool isPointerOverScrollWheelArea;
    private Vector3 lastScrollWheelAreaPos;

    private bool isAllFingersUp;

    private bool isPointerOverMousePadArea;
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

        mousePadArea.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterMousePadArea(evt));
        mousePadArea.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveMousePadArea(evt));
        mousePadArea.RegisterCallback<PointerMoveEvent>(evt => OnPointerMoveOnMousePadArea(evt));
        mousePadArea.RegisterCallback<PointerDownEvent>(evt => OnPointerDownOnMousePadArea(evt));
        mousePadArea.GetRootVisualElement().RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt));

        scrollWheelArea.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerMoveEvent>(evt => OnPointerMoveOnScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerDownEvent>(evt => OnPointerDownOnScrollWheelArea(evt));

        applicationManager.FingerUpEventStream.Subscribe(_ =>
        {
            // The event seems to be fired before the count is decreased.
            // Thus, check for count smaller or equal than 1.
            if (Touch.activeTouches.Count <= 1
                || Touch.activeFingers.Count<= 1)
            {
                isAllFingersUp = true;
            }
        });
    }

    private void SendSimulateLeftMouseButtonClickRequest()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        Debug.Log("simulating left mouse button single click");
        SendSimulateInputRequest("leftMouseButton");
    }

    private void SendSimulateLeftMouseButtonDoubleClickRequest()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        Debug.Log("simulating left mouse button double click");
        SendSimulateInputRequest("leftMouseButton");
        SendSimulateInputRequest("leftMouseButton");
    }

    private void SendSimulateDragStartRequest()
    {
        if (awaitingDragEnd)
        {
            return;
        }

        awaitingDragEnd = true;
        Debug.Log("simulating drag start");
        SendSimulateInputRequest("dragStart");
    }

    private void SendSimulateDragEndRequest()
    {
        if (!awaitingDragEnd)
        {
            return;
        }

        awaitingDragEnd = false;
        Debug.Log("simulating drag end");
        SendSimulateInputRequest("dragEnd");
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        isPointerDownOnMousePadArea = false;

        if (awaitingDragEnd)
        {
            SendSimulateDragEndRequest();
        }
    }

    private void OnPointerDownOnMousePadArea(PointerDownEvent evt)
    {
        UpdateClickCountOnPointerDownOnMousePadArea();

        isAllFingersUp = false;
        isMousePadAreaTotalPointerDeltaAboveThreshold = false;
        isPointerDownOnMousePadArea = true;
        mousePadAreaStartPos = evt.localPosition;
        lastMousePadAreaPos = evt.localPosition;

        // Check for single click, i.e. released all fingers after single click
        if (mousePadAreaPointerDownEventClickCount == 1)
        {
            MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(ClickTimeThresholdInSeconds, () =>
            {
                if (!isPointerDownOnMousePadArea
                    && mousePadAreaPointerDownEventClickCount == 1
                    && !isMousePadAreaTotalPointerDeltaAboveThreshold
                    && !awaitingDragEnd)
                {
                    SendSimulateLeftMouseButtonClickRequest();
                }
            }));
        }

        // Check for double click, i.e. released all fingers after double click
        if (mousePadAreaPointerDownEventClickCount == 2)
        {
            MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(DoubleClickTimeThresholdInSeconds, () =>
            {
                if (!isPointerDownOnMousePadArea
                    && mousePadAreaPointerDownEventClickCount == 2
                    && !isMousePadAreaTotalPointerDeltaAboveThreshold
                    && !awaitingDragEnd)
                {
                    SendSimulateLeftMouseButtonDoubleClickRequest();
                }
            }));
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
        isPointerOverMousePadArea = true;
        mousePadAreaStartPos = evt.localPosition;
        lastMousePadAreaPos = evt.localPosition;
    }

    private void OnPointerLeaveMousePadArea(PointerLeaveEvent evt)
    {
        isPointerOverMousePadArea = false;
    }

    private void OnPointerEnterScrollWheelArea(PointerEnterEvent evt)
    {
        isPointerOverScrollWheelArea = true;
        lastScrollWheelAreaPos = evt.localPosition;
    }

    private void OnPointerLeaveScrollWheelArea(PointerLeaveEvent evt)
    {
        isPointerOverScrollWheelArea = false;
    }

    private void OnPointerMoveOnScrollWheelArea(PointerMoveEvent evt)
    {
        if (!isPointerOverScrollWheelArea)
        {
            lastScrollWheelAreaPos = evt.localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            // All fingers up => reset position
            isAllFingersUp = false;
            lastScrollWheelAreaPos = evt.localPosition;
            return;
        }

        Vector3 pointerDelta = evt.localPosition - lastScrollWheelAreaPos;
        if (Math.Abs(pointerDelta.y) > scrollWheelArea.contentRect.height / 10)
        {
            float deltaX = 0;
            float deltaY = Math.Sign(-pointerDelta.y);
            SendSimulateScrollWheelRequest(new Vector2(deltaX, deltaY));
            lastScrollWheelAreaPos = evt.localPosition;
        }
    }

    private void OnPointerDownOnScrollWheelArea(PointerDownEvent evt)
    {
        lastScrollWheelAreaPos = evt.localPosition;
    }

    private void OnPointerMoveOnMousePadArea(PointerMoveEvent evt)
    {
        if (!isPointerOverMousePadArea
            || isPointerOverScrollWheelArea
            || !isPointerDownOnMousePadArea)
        {
            mousePadAreaStartPos = evt.localPosition;
            lastMousePadAreaPos = evt.localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            // All fingers up => reset position
            isAllFingersUp = false;
            mousePadAreaStartPos = evt.localPosition;
            lastMousePadAreaPos = evt.localPosition;
            return;
        }

        float mousePadAreaMagnitude = mousePadArea.worldBound.size.magnitude;
        if (mousePadAreaMagnitude > 0)
        {
            Vector3 totalPointerDelta = evt.localPosition - mousePadAreaStartPos;
            float magnitudeInPercent = totalPointerDelta.magnitude / mousePadAreaMagnitude;
            Debug.Log($"magnitudeInPercent: {magnitudeInPercent}");
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
            SendSimulateDragStartRequest();
        }

        Vector3 pointerDelta = (evt.localPosition - lastMousePadAreaPos) * settings.MousePadSensitivity;
        SendSimulateMouseDeltaRequest(new Vector2(pointerDelta.x, -pointerDelta.y));
        lastMousePadAreaPos = evt.localPosition;
    }

    private void SendSimulateInputRequest(string inputControl)
    {
        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.Input
            .ReplaceOrThrow("{inputControl}", inputControl));
    }

    private void SendSimulateScrollWheelRequest(Vector2 scrollDelta)
    {
        if (scrollDelta == Vector2.zero)
        {
            return;
        }

        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.InputScrollWheel
            .ReplaceOrThrow("{deltaX}", scrollDelta.x.ToString(CultureInfo.InvariantCulture))
            .ReplaceOrThrow("{deltaY}", scrollDelta.y.ToString(CultureInfo.InvariantCulture)));
    }

    private void SendSimulateMouseDeltaRequest(Vector2 mouseDelta)
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

        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.InputMouseDelta
            .ReplaceOrThrow("{deltaX}", mouseDelta.x.ToStringInvariantCulture())
            .ReplaceOrThrow("{deltaY}", mouseDelta.y.ToStringInvariantCulture()));
    }

    private void RegisterCallbackToSendSimulationInputRequest(Button uiButton, string keyboardButton)
    {
        uiButton.RegisterCallbackButtonTriggered(_ => SendSimulateInputRequest(keyboardButton));
    }
}
