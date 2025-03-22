using System;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputSimulatorRestControl : AbstractRestControl, INeedInjection
{
    public static InputSimulatorRestControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<InputSimulatorRestControl>();

    private Keyboard virtualKeyboard;
    private Mouse virtualMouse;
    private Mouse systemMouse;

    private bool isDragging;
    private bool wasDragging;

    private enum ESimulateButtonDirection
    {
        Down,
        Up,
        DownFollowedByUp
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // Grab the system mouse before the virtual mouse is used.
        // Mouse.current can later change to the virtual mouse.
        systemMouse = Mouse.current;

        virtualKeyboard = InputSystem.AddDevice<Keyboard>("Custom Virtual Keyboard");
        virtualMouse = InputSystem.AddDevice<Mouse>("Custom Virtual Mouse");

        RegisterNavigationEndpoint("leftArrowKey",
            "Simulate left arrow key press",
            virtualKeyboard,
            () => virtualKeyboard.leftArrowKey);

        RegisterNavigationEndpoint("rightArrowKey",
            "Simulate right arrow key press",
            virtualKeyboard,
            () => virtualKeyboard.rightArrowKey);

        RegisterNavigationEndpoint("upArrowKey",
            "Simulate up arrow key press",
            virtualKeyboard,
            () => virtualKeyboard.upArrowKey);

        RegisterNavigationEndpoint("downArrowKey",
            "Simulate down arrow key press",
            virtualKeyboard,
            () => virtualKeyboard.downArrowKey);

        RegisterNavigationEndpoint("enterKey",
            "Simulate enter key press",
            virtualKeyboard,
            () => virtualKeyboard.enterKey);

        RegisterNavigationEndpoint("escapeKey",
            "Simulate escape key press",
            virtualKeyboard,
            () => virtualKeyboard.escapeKey);

        RegisterNavigationEndpoint("spaceKey",
            "Simulate space key press",
            virtualKeyboard,
            () => virtualKeyboard.spaceKey);

        RegisterPseudoNavigationEndpoint("volumeUpKey",
            "Increase volume",
            () => IncreaseVolume());

        RegisterPseudoNavigationEndpoint("volumeDownKey",
            "Decrease volume",
            () => DecreaseVolume());

        RegisterNavigationEndpoint("leftMouseButton",
            "Simulate left mouse button click, i.e. down followed by up",
            virtualMouse,
            () => virtualMouse.leftButton,
            () => !isDragging);

        RegisterNavigationEndpoint("rightMouseButton",
            "Simulate right mouse button click, i.e. down followed by up",
            virtualMouse,
            () => virtualMouse.rightButton,
            () => !isDragging);

        RegisterNavigationEndpoint("middleMouseButton",
            "Simulate middle mouse button click, i.e. down followed by up",
            virtualMouse,
            () => virtualMouse.middleButton,
            () => !isDragging);

        RegisterNavigationEndpointCallback("dragStart",
            "Simulate drag start",
            _ =>
            {
                if (isDragging)
                {
                    return;
                }

                Log.Debug(() => "Received input simulation request 'dragStart'");
                isDragging = true;
            });

        RegisterNavigationEndpointCallback("dragEnd",
            "Simulate drag end",
            _ =>
            {
                if (!isDragging)
                {
                    return;
                }

                Log.Debug(() => "Received input simulation request 'dragEnd'");
                isDragging = false;
            });

        RegisterMouseDeltaEndpoint();
        RegisterScrollWheelEndpoint();
    }

    private void Update()
    {
        UpdateMouseDragSimulation();
    }

    protected override void OnDestroySingleton()
    {
        InputSystem.RemoveDevice(virtualKeyboard);
        InputSystem.RemoveDevice(virtualMouse);
    }

    private void UpdateMouseDragSimulation()
    {
        // TODO: Simulating drag does not seem to work.
        // Unity always seems to do a mouse click instead of holding the button.
        // See https://forum.unity.com/threads/how-to-simulate-drag-event-with-new-inputsystem.1525885/
        return;

        // if (isDragging)
        // {
        //     // Keep writing a 1 to the InputControl
        //     using (StateEvent.From(virtualMouse, out InputEventPtr eventPtr))
        //     {
        //         virtualMouse.leftButton.WriteValueIntoEvent(1f, eventPtr);
        //         InputSystem.QueueEvent(eventPtr);
        //     }
        // }
        // else if (wasDragging)
        // {
        //     // Write a 0 to the InputControl once
        //     using (StateEvent.From(virtualMouse, out InputEventPtr eventPtr))
        //     {
        //         virtualMouse.leftButton.WriteValueIntoEvent(0f, eventPtr);
        //         InputSystem.QueueEvent(eventPtr);
        //     }
        // }
        //
        // wasDragging = isDragging;
    }

    private void IncreaseVolume()
    {
        SettingsUtils.IncreaseVolume(settings);
    }

    private void DecreaseVolume()
    {
        SettingsUtils.DecreaseVolume(settings);
    }

    /**
     * Method that uses an endpoint similar to other input simulation,
     * but for a key that Unity does not really support.
     */
    private void RegisterPseudoNavigationEndpoint(string inputControlName, string description, Action callback)
    {
        string path = $"api/rest/input/{inputControlName}";
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription(description)
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteInputSimulation, settings)
            .SetCallbackAndAdd(requestData =>
            {
                if (virtualKeyboard == null)
                {
                    return;
                }

                Log.Debug(() => $"Received input simulation request '{path}' via URL '{requestData.Context.Request.Url}'");
                callback?.Invoke();
            });
    }

    private void RegisterMouseDeltaEndpoint()
    {
        string path = RestApiEndpointPaths.InputMouseDelta;
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription("Move the current mouse if any by the given X and Y delta values")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteInputSimulation, settings)
            .SetCallbackAndAdd(requestData =>
            {
                Log.Verbose(() => $"Received input simulation request '{path}' via URL '{requestData.Context.Request.Url}'");
                bool hasDeltaX = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaX"], out double deltaX);
                bool hasDeltaY = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaY"], out double deltaY);
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateMouseDelta(new Vector2((float)deltaX, (float)deltaY));
                }
            });
    }

    private void RegisterScrollWheelEndpoint()
    {
        string path = RestApiEndpointPaths.InputScrollWheel;
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription("Simulate scroll wheel events")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteInputSimulation, settings)
            .SetCallbackAndAdd(requestData =>
            {
                Log.Debug(() => $"Received input simulation request '{path}' via URL '{requestData.Context.Request.Url}'");
                bool hasDeltaX = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaX"], out double deltaX);
                bool hasDeltaY = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaY"], out double deltaY);
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateMouseScrollDelta(new Vector2((float)deltaX, (float)deltaY));
                }
            });
    }

    private async void SimulateMouseScrollDelta(Vector2 scrollDelta)
    {
        await Awaitable.MainThreadAsync();

        using (StateEvent.From(systemMouse, out InputEventPtr eventPtr))
        {
            systemMouse.scroll.WriteValueIntoEvent(scrollDelta, eventPtr);
            InputSystem.QueueEvent(eventPtr);
        }
    }

    private async void SimulateMouseDelta(Vector2 delta)
    {
        if (delta == Vector2.zero)
        {
            return;
        }

        await Awaitable.MainThreadAsync();

        // Use the mouse position from the legacy input API
        // because the value returned by Unity's newer InputSystem is not accurate when executed in the Unity editor.
        Vector2 unityInputMousePosition = Input.mousePosition;
        Vector2 newMousePosition = unityInputMousePosition + delta;
        using (StateEvent.From(systemMouse, out InputEventPtr eventPtr))
        {
            systemMouse.WarpCursorPosition(newMousePosition);
            InputSystem.QueueEvent(eventPtr);
        }
    }

    private void RegisterNavigationEndpointCallback(
        string inputControlName,
        string description,
        Action<EndpointRequestData> callback)
    {
        string path = $"api/rest/input/{inputControlName}";
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription(description)
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteInputSimulation, settings)
            .SetCallbackAndAdd(request =>
            {
                callback(request);
            });
    }

    private void RegisterNavigationEndpoint(
        string inputControlName,
        string description,
        InputDevice inputDevice,
        Func<InputControl> inputControlGetter,
        Func<bool> condition = null,
        ESimulateButtonDirection simulateButtonDirection = ESimulateButtonDirection.DownFollowedByUp)
    {
        string path = $"api/rest/input/{inputControlName}";
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription(description)
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(RestApiPermission.WriteInputSimulation, settings)
            .SetCallbackAndAdd(_ =>
            {
                if (condition != null
                    && !condition())
                {
                    return;
                }

                InputControl inputControl = inputControlGetter();
                Log.Debug(() => $"Received input simulation request {path}");
                SimulateButtonClick(inputDevice, inputControl, simulateButtonDirection);
            });
    }

    private async void SimulateButtonClick(InputDevice inputDevice, InputControl inputControl, ESimulateButtonDirection simulateButtonDirection)
    {
        Log.Debug(() => $"Triggering button event {simulateButtonDirection} on input control {inputControl}");

        if (simulateButtonDirection
            is ESimulateButtonDirection.Down
            or ESimulateButtonDirection.DownFollowedByUp)
        {
            await Awaitable.MainThreadAsync();

            using (StateEvent.From(inputDevice, out InputEventPtr eventPtr))
            {
                inputControl.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        if (simulateButtonDirection
            is ESimulateButtonDirection.Up
            or ESimulateButtonDirection.DownFollowedByUp)
        {
            await Awaitable.MainThreadAsync();
            await Awaitable.EndOfFrameAsync();

            using (StateEvent.From(inputDevice, out InputEventPtr eventPtr))
            {
                inputControl.WriteValueIntoEvent(0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
}
