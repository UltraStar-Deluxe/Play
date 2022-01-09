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
using UnityEngine.UIElements;

public class UltraStarPlayInputManager : InputManager, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        AdditionalInputActionInfos.Clear();
    }

    public static List<InputActionInfo> AdditionalInputActionInfos { get; private set; }= new List<InputActionInfo>();

    [InjectedInInspector]
    public VectorImage gamepadIcon;

    [InjectedInInspector]
    public VectorImage keyboardAndMouseIcon;

    [InjectedInInspector]
    public VectorImage touchIcon;

    private Subject<InputDeviceChangeEvent> inputDeviceChangeEventStream = new Subject<InputDeviceChangeEvent>();
    public IObservable<InputDeviceChangeEvent> InputDeviceChangeEventStream => inputDeviceChangeEventStream;

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
        ContextMenu.OpenContextMenus.Clear();

        if (inputDeviceIcon != null)
        {
            InputDeviceChangeEventStream.Subscribe(_ => UpdateInputDeviceIcon());
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
        Debug.Log($"inputDeviceChange: {inputDeviceChange}");
        inputDeviceChangeEventStream.OnNext(new InputDeviceChangeEvent(inputDevice, inputDeviceChange));
    }

    public static EInputDevice GetCurrentInputDeviceEnum()
    {
        if (Gamepad.current != null
            && Gamepad.current.enabled)
        {
            return EInputDevice.Gamepad;
        }

        if (Keyboard.current != null
            && Keyboard.current.enabled)
        {
            return EInputDevice.KeyboardAndMouse;
        }

        return EInputDevice.Touch;
    }

    private void UpdateInputDeviceIcon()
    {
        switch (GetCurrentInputDeviceEnum())
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
        }
    }
}
