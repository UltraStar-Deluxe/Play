// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.
using UnityEngine.InputSystem;

public class InputActions
{
    public UIInputActions UI { get; private set; }
    public USPlayInputActions USPlay { get; private set; }

    public InputActions(InputActionAsset inputActionAsset)
    {
        UI = new UIInputActions(inputActionAsset);
        USPlay = new USPlayInputActions(inputActionAsset);
    }

    public class UIInputActions
    {
        public InputAction NavigateAction { get; private set; }
        public InputAction SubmitAction { get; private set; }
        public InputAction CancelAction { get; private set; }
        public InputAction TabAction { get; private set; }
        public InputAction PointAction { get; private set; }
        public InputAction ClickAction { get; private set; }
        public InputAction ScrollWheelAction { get; private set; }
        public InputAction MiddleClickAction { get; private set; }
        public InputAction RightClickAction { get; private set; }
        public InputAction TrackedDevicePositionAction { get; private set; }
        public InputAction TrackedDeviceOrientationAction { get; private set; }

        public UIInputActions(InputActionAsset inputActionAsset)
        {
            NavigateAction = inputActionAsset.FindAction("UI/Navigate", true);
            SubmitAction = inputActionAsset.FindAction("UI/Submit", true);
            CancelAction = inputActionAsset.FindAction("UI/Cancel", true);
            TabAction = inputActionAsset.FindAction("UI/Tab", true);
            PointAction = inputActionAsset.FindAction("UI/Point", true);
            ClickAction = inputActionAsset.FindAction("UI/Click", true);
            ScrollWheelAction = inputActionAsset.FindAction("UI/ScrollWheel", true);
            MiddleClickAction = inputActionAsset.FindAction("UI/MiddleClick", true);
            RightClickAction = inputActionAsset.FindAction("UI/RightClick", true);
            TrackedDevicePositionAction = inputActionAsset.FindAction("UI/TrackedDevicePosition", true);
            TrackedDeviceOrientationAction = inputActionAsset.FindAction("UI/TrackedDeviceOrientation", true);
        }
    }

    public class USPlayInputActions
    {
        public InputAction BackAction { get; private set; }

        public USPlayInputActions(InputActionAsset inputActionAsset)
        {
            BackAction = inputActionAsset.FindAction("USPlay/Back", true);
        }
    }

}
