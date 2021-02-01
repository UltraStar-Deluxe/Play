// GENERATED CODE. To update this file use the corresponding menu item in the Unity Editor.
using UnityEngine;

using UnityEngine.InputSystem;

public class InputActions
{
    public UIInputActions UI { get; private set; }
    public USPlayInputActions USPlay { get; private set; }

    public InputActions(InputActionAsset inputActionAsset, GameObject owner)
    {
        UI = new UIInputActions(inputActionAsset, owner);
        USPlay = new USPlayInputActions(inputActionAsset, owner);
    }

    public class UIInputActions
    {
        public ObservableInputAction Navigate { get; private set; }
        public ObservableInputAction Submit { get; private set; }
        public ObservableInputAction Cancel { get; private set; }
        public ObservableInputAction Tab { get; private set; }
        public ObservableInputAction Point { get; private set; }
        public ObservableInputAction Click { get; private set; }
        public ObservableInputAction ScrollWheel { get; private set; }
        public ObservableInputAction MiddleClick { get; private set; }
        public ObservableInputAction RightClick { get; private set; }
        public ObservableInputAction TrackedDevicePosition { get; private set; }
        public ObservableInputAction TrackedDeviceOrientation { get; private set; }

        public UIInputActions(InputActionAsset inputActionAsset, GameObject owner)
        {
            Navigate = new ObservableInputAction(inputActionAsset.FindAction("UI/Navigate", true), owner);
            Submit = new ObservableInputAction(inputActionAsset.FindAction("UI/Submit", true), owner);
            Cancel = new ObservableInputAction(inputActionAsset.FindAction("UI/Cancel", true), owner);
            Tab = new ObservableInputAction(inputActionAsset.FindAction("UI/Tab", true), owner);
            Point = new ObservableInputAction(inputActionAsset.FindAction("UI/Point", true), owner);
            Click = new ObservableInputAction(inputActionAsset.FindAction("UI/Click", true), owner);
            ScrollWheel = new ObservableInputAction(inputActionAsset.FindAction("UI/ScrollWheel", true), owner);
            MiddleClick = new ObservableInputAction(inputActionAsset.FindAction("UI/MiddleClick", true), owner);
            RightClick = new ObservableInputAction(inputActionAsset.FindAction("UI/RightClick", true), owner);
            TrackedDevicePosition = new ObservableInputAction(inputActionAsset.FindAction("UI/TrackedDevicePosition", true), owner);
            TrackedDeviceOrientation = new ObservableInputAction(inputActionAsset.FindAction("UI/TrackedDeviceOrientation", true), owner);
        }
    }

    public class USPlayInputActions
    {
        public ObservableInputAction Back { get; private set; }

        public USPlayInputActions(InputActionAsset inputActionAsset, GameObject owner)
        {
            Back = new ObservableInputAction(inputActionAsset.FindAction("USPlay/Back", true), owner);
        }
    }

}
