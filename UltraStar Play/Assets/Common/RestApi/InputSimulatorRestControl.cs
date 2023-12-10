using System;
using System.Linq;
using System.Net.Http;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputSimulatorRestControl : AbstractRestControl, INeedInjection
{
    public static InputSimulatorRestControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<InputSimulatorRestControl>();

    private Keyboard virtualKeyboard;
    private Mouse virtualMouse;
    private Mouse systemMouse;

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
            () => virtualMouse.leftButton);

        RegisterNavigationEndpoint("rightMouseButton",
            "Simulate right mouse button click, i.e. down followed by up",
            virtualMouse,
            () => virtualMouse.rightButton);

        RegisterNavigationEndpoint("middleMouseButton",
            "Simulate middle mouse button click, i.e. down followed by up",
            virtualMouse,
            () => virtualMouse.middleButton);

        RegisterNavigationEndpoint("leftMouseButtonDown",
            "Simulate left mouse button down event",
            virtualMouse,
            () => virtualMouse.leftButton,
            ESimulateButtonDirection.Down);

        RegisterNavigationEndpoint("leftMouseButtonUp",
            "Simulate left mouse button up event",
            virtualMouse,
            () => virtualMouse.leftButton,
            ESimulateButtonDirection.Up);

        RegisterMouseDeltaEndpoint();
        RegisterScrollWheelEndpoint();
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
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
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
        string path = HttpApiEndpointPaths.InputMouseDelta;
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription("Move the current mouse if any by the given X and Y delta values")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(requestData =>
            {
                Log.Debug(() => $"Received input simulation request '{path}' via URL '{requestData.Context.Request.Url}'");
                bool hasDeltaX = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaX"], out double deltaX);
                bool hasDeltaY = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaY"], out double deltaY);
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateSystemMouseDelta(new Vector2((float)deltaX, (float)deltaY));
                }
            });
    }

    private void RegisterScrollWheelEndpoint()
    {
        string path = HttpApiEndpointPaths.InputScrollWheel;
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription("Simulate scroll wheel events")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(requestData =>
            {
                Log.Debug(() => $"Received input simulation request '{path}' via URL '{requestData.Context.Request.Url}'");
                bool hasDeltaX = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaX"], out double deltaX);
                bool hasDeltaY = NumberUtils.TryParseDoubleAnyCulture(requestData.PathParameters["deltaY"], out double deltaY);
                Debug.Log($"deltaX: {deltaX} | deltaY: {deltaY}");
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateVirtualMouseScrollDelta(new Vector2((float)deltaX, (float)deltaY));
                }
            });
    }

    private void SimulateVirtualMouseScrollDelta(Vector2 scrollDelta)
    {
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
        {
            using (StateEvent.From(virtualMouse, out InputEventPtr eventPtr))
            {
                virtualMouse.scroll.WriteValueIntoEvent(scrollDelta, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }));
    }

    private void SimulateSystemMouseDelta(Vector2 delta)
    {
        if (delta == Vector2.zero)
        {
            return;
        }

        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
        {
            DoSimulateMouseDelta(delta, systemMouse);
        }));
    }

    private void DoSimulateMouseDelta(Vector2 delta, Mouse mouse)
    {
        // Use the mouse position from the legacy input API
        // because the value returned by Unity's newer InputSystem is not accurate when executed in the Unity editor.
        Vector2 unityInputMousePosition = Input.mousePosition;
        Vector2 newMousePosition = unityInputMousePosition + delta;
        using (StateEvent.From(mouse, out InputEventPtr eventPtr))
        {
            mouse.WarpCursorPosition(newMousePosition);
            InputSystem.QueueEvent(eventPtr);
        }
    }

    private void RegisterNavigationEndpoint(
        string inputControlName,
        string description,
        InputDevice inputDevice,
        Func<InputControl> inputControlGetter,
        ESimulateButtonDirection simulateButtonDirection = ESimulateButtonDirection.DownFollowedByUp)
    {
        string path = $"api/rest/input/{inputControlName}";
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription(description)
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(_ =>
            {
                InputControl inputControl = inputControlGetter();
                Log.Debug(() => $"Received input simulation request {path}");
                SimulateButtonClick(inputDevice, inputControl, simulateButtonDirection);
            });
    }

    private void SimulateButtonClick(InputDevice inputDevice, InputControl inputControl, ESimulateButtonDirection simulateButtonDirection)
    {
        Log.Debug(() => $"Triggering button event {simulateButtonDirection} on input control {inputControl}");

        if (simulateButtonDirection
            is ESimulateButtonDirection.Down
            or ESimulateButtonDirection.DownFollowedByUp)
        {
            MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
            {
                using (StateEvent.From(inputDevice, out InputEventPtr eventPtr))
                {
                    inputControl.WriteValueIntoEvent(1f, eventPtr);
                    InputSystem.QueueEvent(eventPtr);
                }
            }));
        }

        if (simulateButtonDirection
            is ESimulateButtonDirection.Up
            or ESimulateButtonDirection.DownFollowedByUp)
        {
            MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () =>
            {
                using (StateEvent.From(inputDevice, out InputEventPtr eventPtr))
                {
                    inputControl.WriteValueIntoEvent(0f, eventPtr);
                    InputSystem.QueueEvent(eventPtr);
                }
            }));
        }
    }
}
