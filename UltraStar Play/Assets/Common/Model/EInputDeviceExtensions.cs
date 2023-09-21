using System;
using UnityEngine.InputSystem;

public static class EInputDeviceExtensions
{
    public static InputDevice GetInputDevice(this EInputDevice inputDevice)
    {
        switch (inputDevice)
        {
            case EInputDevice.KeyboardAndMouse:
                return Keyboard.current;
            case EInputDevice.Gamepad:
                return Gamepad.current;
            case EInputDevice.Touch:
                return Touchscreen.current;
            default:
                throw new ArgumentException($"No enum value defined for InputDevice {inputDevice}");
        }
    }

    public static EInputDevice GetInputDeviceEnum(this InputDevice inputDevice)
    {
        if (inputDevice == Keyboard.current)
        {
            return EInputDevice.KeyboardAndMouse;
        }

        if (inputDevice == Gamepad.current)
        {
            return EInputDevice.Gamepad;
        }

        if (inputDevice == Touchscreen.current)
        {
            return EInputDevice.Touch;
        }
        throw new ArgumentException($"No enum value defined for InputDevice {inputDevice}");
    }
}
