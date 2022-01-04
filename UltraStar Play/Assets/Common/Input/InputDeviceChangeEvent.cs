using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceChangeEvent
{
    public InputDevice InputDevice { get; private set; }
    public InputDeviceChange InputDeviceChange { get; private set; }

    public InputDeviceChangeEvent(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
    {
        this.InputDevice = inputDevice;
        this.InputDeviceChange = inputDeviceChange;
    }
}
