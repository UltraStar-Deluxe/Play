using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UltraStarPlayInputManager : InputManager, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        AdditionalInputActionInfos.Clear();
        inputDeviceToLastChange.Clear();
    }

    public static List<InputActionInfo> AdditionalInputActionInfos { get; private set; } = new List<InputActionInfo>();
    private static readonly Dictionary<InputDevice, InputDeviceChange> inputDeviceToLastChange = new Dictionary<InputDevice, InputDeviceChange>();

    [InjectedInInspector]
    public VectorImage gamepadIcon;

    [InjectedInInspector]
    public VectorImage keyboardAndMouseIcon;

    [InjectedInInspector]
    public VectorImage touchIcon;

    private readonly Subject<InputDeviceChangeEvent> inputDeviceChangeEventStream = new Subject<InputDeviceChangeEvent>();
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

    [Inject(UxmlName = R.UxmlNames.inputDeviceIcon, Optional = true)]
    private VisualElement inputDeviceIcon;

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

        if (inputDeviceIcon != null)
        {
            UpdateInputDeviceIcon();
            InputDeviceChangeEventStream.Subscribe(_ => UpdateInputDeviceIcon());
        }

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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (InputManager.instance == this)
        {
            AdditionalInputActionInfos.Clear();
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

    private static bool IsLastInputDeviceChangeOneOf(InputDevice inputDevice, params InputDeviceChange[] values)
    {
        if (!inputDeviceToLastChange.TryGetValue(inputDevice, out InputDeviceChange inputDeviceChange))
        {
            return false;
        }

        return values.Contains(inputDeviceChange);
    }

    private void UpdateInputDeviceIcon()
    {
        switch (inputDeviceEnum)
        {
            case EInputDevice.Gamepad:
                inputDeviceIcon.style.backgroundImage = new StyleBackground(gamepadIcon);
                break;
            case EInputDevice.KeyboardAndMouse:
                inputDeviceIcon.style.backgroundImage = new StyleBackground(keyboardAndMouseIcon);
                break;
            case EInputDevice.Touch:
                inputDeviceIcon.style.backgroundImage = new StyleBackground(touchIcon);
                break;
            default:
                Debug.Log("Unhandled EInputDevice: " + inputDeviceEnum);
                break;
        }
    }
}
