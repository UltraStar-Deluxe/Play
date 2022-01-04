using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using PrimeInputActions;
using UniRx;

public class UltraStarPlayInputManager : InputManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        AdditionalInputActionInfos.Clear();
    }

    public static List<InputActionInfo> AdditionalInputActionInfos { get; private set; }= new List<InputActionInfo>();

    private Subject<InputDeviceChangeEvent> inputDeviceChangeEventStream = new Subject<InputDeviceChangeEvent>();
    public IObservable<InputDeviceChangeEvent> InputDeviceChangeEventStream => inputDeviceChangeEventStream;

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
        inputDeviceChangeEventStream.OnNext(new InputDeviceChangeEvent(inputDevice, inputDeviceChange));
    }

    public static EInputDevice GetCurrentInputDeviceEnum()
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
}
