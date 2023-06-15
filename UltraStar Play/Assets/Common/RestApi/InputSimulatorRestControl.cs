using System;
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

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        // Grab the system mouse before the virtual mouse is used.
        // Mouse.current can later change to the virtual mouse.
        systemMouse = Mouse.current;
        
        virtualKeyboard = InputSystem.AddDevice<Keyboard>("Virtual Keyboard");
        virtualMouse = InputSystem.AddDevice<Mouse>("Virtual Mouse");

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
            "Simulate left mouse button press",
            virtualMouse,
            () => virtualMouse.leftButton);
        
        RegisterNavigationEndpoint("rightMouseButton",
            "Simulate left mouse button press",
            virtualMouse,
            () => virtualMouse.rightButton);
        
        RegisterNavigationEndpoint("middleMouseButton",
            "Simulate left mouse button press",
            virtualMouse,
            () => virtualMouse.middleButton);

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

                Debug.Log($"Received input simulation request {path}");
                callback?.Invoke();
            });
    }

    private void RegisterMouseDeltaEndpoint()
    {
        httpServer.CreateEndpoint(HttpMethod.Post, HttpApiEndpointPaths.InputMouseDelta)
            .SetDescription("Move the current mouse if any by the given X and Y delta values")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(requestData =>
            {
                if (systemMouse == null)
                {
                    return;
                }

                bool hasDeltaX = float.TryParse(requestData.PathParameters["deltaX"], out float deltaX);
                bool hasDeltaY = float.TryParse(requestData.PathParameters["deltaY"], out float deltaY);
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateCurrentMouseDelta(new Vector2(deltaX, deltaY));
                }
            });
    }

    private void RegisterScrollWheelEndpoint()
    {
        httpServer.CreateEndpoint(HttpMethod.Post, HttpApiEndpointPaths.InputScrollWheel)
            .SetDescription("Simulate scroll wheel events")
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(requestData =>
            {
                if (systemMouse == null)
                {
                    return;
                }

                bool hasDeltaX = float.TryParse(requestData.PathParameters["deltaX"], out float deltaX);
                bool hasDeltaY = float.TryParse(requestData.PathParameters["deltaY"], out float deltaY);
                if (hasDeltaX && hasDeltaY)
                {
                    SimulateVirtualMouseScrollDelta(new Vector2(deltaX, deltaY));
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

    private void SimulateCurrentMouseDelta(Vector2 delta)
    {
        if (delta == Vector2.zero)
        {
            return;
        }

        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
        {
            if (systemMouse == null)
            {
                return;
            }
            Vector2 currentMousePosition = systemMouse.position.ReadValue();
            Vector2 newMousePosition = currentMousePosition + delta;
            using (StateEvent.From(systemMouse, out InputEventPtr eventPtr))
            {
                systemMouse.WarpCursorPosition(newMousePosition);
                InputSystem.QueueEvent(eventPtr);
            }
        }));
    }

    private void RegisterNavigationEndpoint(string inputControlName, string description, InputDevice inputDevice, Func<InputControl> inputControlGetter)
    {
        string path = $"api/rest/input/{inputControlName}";
        httpServer.CreateEndpoint(HttpMethod.Post, path)
            .SetDescription(description)
            .SetRemoveOnDestroy(gameObject)
            .SetRequiredPermission(HttpApiPermission.WriteInputSimulation)
            .SetCallbackAndAdd(_ =>
            {
                InputControl inputControl = inputControlGetter();
                Debug.Log($"Received input simulation request {path}");
                SimulateButtonClick(inputDevice, inputControl);
            });
    }

    private void SimulateButtonClick(InputDevice inputDevice, InputControl inputControl)
    {
        Debug.Log($"Triggering button click on input control {inputControl} by setting its value to 1 and afterwards to 0");
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(0, () =>
        {
            using (StateEvent.From(inputDevice, out InputEventPtr eventPtr))
            {
                inputControl.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }));
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
