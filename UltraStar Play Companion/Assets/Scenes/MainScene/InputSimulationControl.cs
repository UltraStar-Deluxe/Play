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
    private Vector3 scrollWheelAreaStartPos;

    private bool isAllFingersUp;
    private bool isPointerOverMousePadArea;
    private Vector3 mousePadAreaStartPos;

    private float mousePadAreaPointerDownStartTimeInSeconds;
    
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
        mousePadArea.RegisterCallback<PointerDownEvent>(evt =>
        {
            mousePadAreaPointerDownStartTimeInSeconds = Time.time;
        });
        mousePadArea.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (mousePadAreaPointerDownStartTimeInSeconds > 0
                && !isPointerOverScrollWheelArea
                && (Time.time - mousePadAreaPointerDownStartTimeInSeconds) < ClickTimeThresholdInSeconds)
            {
                SendSimulateInputRequest("leftMouseButton");
            }
            mousePadAreaPointerDownStartTimeInSeconds = 0;
        });
        
        scrollWheelArea.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnterScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerLeaveEvent>(evt => OnPointerLeaveScrollWheelArea(evt));
        scrollWheelArea.RegisterCallback<PointerMoveEvent>(evt => OnPointerMoveOnScrollWheelArea(evt));

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

    private void OnPointerEnterMousePadArea(PointerEnterEvent evt)
    {
        isPointerOverMousePadArea = true;
        mousePadAreaStartPos = evt.localPosition;
    }
    
    private void OnPointerLeaveMousePadArea(PointerLeaveEvent evt)
    {
        isPointerOverMousePadArea = false;
    }

    private void OnPointerEnterScrollWheelArea(PointerEnterEvent evt)
    {
        isPointerOverScrollWheelArea = true;
        scrollWheelAreaStartPos = evt.localPosition;
    }

    private void OnPointerLeaveScrollWheelArea(PointerLeaveEvent evt)
    {
        isPointerOverScrollWheelArea = false;
    }

    private void OnPointerMoveOnScrollWheelArea(PointerMoveEvent evt)
    {
        if (!isPointerOverScrollWheelArea)
        {
            scrollWheelAreaStartPos = evt.localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            // All fingers up => reset position
            isAllFingersUp = false;
            scrollWheelAreaStartPos = evt.localPosition;
            mousePadAreaStartPos = evt.localPosition;
        }
        
        Vector3 pointerDelta = evt.localPosition - scrollWheelAreaStartPos;
        if (Math.Abs(pointerDelta.y) > scrollWheelArea.contentRect.height / 10)
        {
            float deltaX = 0;
            float deltaY = Math.Sign(-pointerDelta.y);
            SendSimulateScrollWheelRequest(new Vector2(deltaX, deltaY));
            scrollWheelAreaStartPos = evt.localPosition;
        }
    }

    private void OnPointerMoveOnMousePadArea(PointerMoveEvent evt)
    {
        if (!isPointerOverMousePadArea
            || isPointerOverScrollWheelArea)
        {
            mousePadAreaStartPos = evt.localPosition;
            return;
        }

        if (isAllFingersUp)
        {
            // All fingers up => reset position
            isAllFingersUp = false;
            scrollWheelAreaStartPos = evt.localPosition;
            mousePadAreaStartPos = evt.localPosition;
        }
        Vector3 pointerDelta = (evt.localPosition - mousePadAreaStartPos) * settings.mousePadSensitivity;
        SendSimulateMouseDeltaRequest(new Vector2(pointerDelta.x, -pointerDelta.y));
        mousePadAreaStartPos = evt.localPosition;
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

        mainGameHttpClient.PostRequest(HttpApiEndpointPaths.InputMouseDelta
            .ReplaceOrThrow("{deltaX}", mouseDelta.x.ToStringInvariantCulture())
            .ReplaceOrThrow("{deltaY}", mouseDelta.y.ToStringInvariantCulture()));
    }

    private void RegisterCallbackToSendSimulationInputRequest(Button uiButton, string keyboardButton)
    {
        uiButton.RegisterCallbackButtonTriggered(_ => SendSimulateInputRequest(keyboardButton));
    }
}
