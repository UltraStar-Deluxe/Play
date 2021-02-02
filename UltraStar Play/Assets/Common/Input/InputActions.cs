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
        public ObservableCancelablePriorityInputAction Navigate { get; private set; }
        public ObservableCancelablePriorityInputAction Submit { get; private set; }
        public ObservableCancelablePriorityInputAction Cancel { get; private set; }
        public ObservableCancelablePriorityInputAction Tab { get; private set; }
        public ObservableCancelablePriorityInputAction Point { get; private set; }
        public ObservableCancelablePriorityInputAction Click { get; private set; }
        public ObservableCancelablePriorityInputAction ScrollWheel { get; private set; }
        public ObservableCancelablePriorityInputAction MiddleClick { get; private set; }
        public ObservableCancelablePriorityInputAction RightClick { get; private set; }
        public ObservableCancelablePriorityInputAction TrackedDevicePosition { get; private set; }
        public ObservableCancelablePriorityInputAction TrackedDeviceOrientation { get; private set; }

        public UIInputActions(InputActionAsset inputActionAsset, GameObject owner)
        {
            Navigate = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Navigate", true), owner);
            Submit = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Submit", true), owner);
            Cancel = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Cancel", true), owner);
            Tab = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Tab", true), owner);
            Point = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Point", true), owner);
            Click = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/Click", true), owner);
            ScrollWheel = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/ScrollWheel", true), owner);
            MiddleClick = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/MiddleClick", true), owner);
            RightClick = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/RightClick", true), owner);
            TrackedDevicePosition = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/TrackedDevicePosition", true), owner);
            TrackedDeviceOrientation = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("UI/TrackedDeviceOrientation", true), owner);
        }
    }

    public class USPlayInputActions
    {
        public ObservableCancelablePriorityInputAction Back { get; private set; }
        public ObservableCancelablePriorityInputAction AnyKeyboardModifier { get; private set; }

        public USPlayInputActions(InputActionAsset inputActionAsset, GameObject owner)
        {
            Back = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("USPlay/Back", true), owner);
            AnyKeyboardModifier = new ObservableCancelablePriorityInputAction(inputActionAsset.FindAction("USPlay/AnyKeyboardModifier", true), owner);
        }
    }

}
