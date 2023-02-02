using UnityEngine.InputSystem;

public class InputDeviceManager : AbstractSingletonBehaviour
{
    public static InputDeviceManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<InputDeviceManager>();
    
    // Current keyboard and mouse can be changed to virtual devices (from input simulation).
    // Thus, grab the system keyboard and mouse at startup.
    public Keyboard SystemKeyboard { get; private set; }
    public Mouse SystemMouse { get; private set; }
    public Pointer SystemPointer { get; private set; }
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        SystemKeyboard = Keyboard.current;
        SystemMouse = Mouse.current;
        SystemPointer = Pointer.current;
    }
}
