using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UltraStarPlayInputManager : InputManager, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        inputDeviceToLastChange.Clear();
    }

    private static readonly Dictionary<InputDevice, InputDeviceChange> inputDeviceToLastChange = new();

    private readonly Subject<InputDeviceChangeEvent> inputDeviceChangeEventStream = new();
    public IObservable<InputDeviceChangeEvent> InputDeviceChangeEventStream => inputDeviceChangeEventStream;

    private EInputDevice inputDeviceEnum = GetDefaultInputDeviceEnum();
    public EInputDevice InputDeviceEnum {
        get
        {
            return inputDeviceEnum;
        }
        private set
        {
            if (inputDeviceEnum != value)
            {
                inputDeviceEnum = value;
                inputDeviceChangeEventStream.OnNext(new InputDeviceChangeEvent());
            }
        }
    }

    [Inject(UxmlName = R.UxmlNames.inputDeviceIconKeyboardAndMouse, Optional = true)]
    private VisualElement inputDeviceIconKeyboardAndMouse;

    [Inject(UxmlName = R.UxmlNames.inputDeviceIconGamepad, Optional = true)]
    private VisualElement inputDeviceIconGamepad;

    [Inject(UxmlName = R.UxmlNames.inputDeviceIconTouch, Optional = true)]
    private VisualElement inputDeviceIconTouch;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        try
        {
            // Enable EnhancedTouchSupport to make use of EnhancedTouch.Touch struct etc.
            EnhancedTouchSupport.Enable();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Could not enable enhanced touch support");
        }

        UpdateInputDeviceIcon();
        InputDeviceChangeEventStream.Subscribe(_ => UpdateInputDeviceIcon());

        StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(0.1f, () => UpdateInputDeviceEnum()));
    }

    private void UpdateInputDeviceEnum()
    {
        // Check any keyboard input
        if (Keyboard.current != null
            && Keyboard.current.anyKey.isPressed)
        {
            InputDeviceEnum = EInputDevice.KeyboardAndMouse;
        }

        // Check any gamepad input (of all gamepads)
        bool anyGamepadButtonPressed = Gamepad.current != null
                                       && Gamepad.all.AnyMatch(gamepad => gamepad.allControls.Any(inputControl => inputControl is ButtonControl button
                                           && button.isPressed
                                           && !button.synthetic));
        if (anyGamepadButtonPressed)
        {
            InputDeviceEnum = EInputDevice.Gamepad;
        }

        // Check any touch gesture
        if (Touch.activeTouches.Count > 0)
        {
            InputDeviceEnum = EInputDevice.Touch;
        }
    }

    private void OnDeviceChange(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
    {
        inputDeviceToLastChange[inputDevice] = inputDeviceChange;
        inputDeviceChangeEventStream.OnNext(new InputDeviceChangeEvent());
    }

    private static EInputDevice GetDefaultInputDeviceEnum()
    {
        if (Gamepad.current != null)
        {
            return EInputDevice.Gamepad;
        }

        if (Keyboard.current != null)
        {
            return EInputDevice.KeyboardAndMouse;
        }

        return EInputDevice.Touch;
    }

    private void UpdateInputDeviceIcon()
    {
        inputDeviceIconKeyboardAndMouse?.SetVisibleByDisplay(inputDeviceEnum == EInputDevice.KeyboardAndMouse);
        inputDeviceIconGamepad?.SetVisibleByDisplay(inputDeviceEnum == EInputDevice.Gamepad);
        inputDeviceIconTouch?.SetVisibleByDisplay(inputDeviceEnum == EInputDevice.Touch);
    }
}
