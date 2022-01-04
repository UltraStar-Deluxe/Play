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
                throw new ArgumentException($"No device defined for inputSource {inputDevice}");
        }
    }
}
