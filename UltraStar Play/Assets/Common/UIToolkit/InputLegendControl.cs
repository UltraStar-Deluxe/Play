using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public static class InputLegendControl
{
    /**
     * Adds information how the InputAction can be triggered.
     * But only if there is a binding for the InputAction on a connected InputDevice (Gamepad, Keyboard, ...).
     *
     * Returns the created VisualElement or null.
     */
    public static VisualElement TryAddInputActionInfo(string inputActionPath, string actionText, VisualElement targetVisualElement)
    {
        if (targetVisualElement == null)
        {
            return null;
        }

        InputDevice inputDevice = UltraStarPlayInputManager.GetCurrentInputDeviceEnum().GetInputDevice();;
        InputAction inputAction = InputManager.GetInputAction(inputActionPath).InputAction;
        string bindingDisplayString = GetBindingDisplayString(inputAction, inputDevice);
        if (bindingDisplayString.IsNullOrEmpty())
        {
            return null;
        }

        InputActionInfo inputActionInfo = new InputActionInfo(actionText, bindingDisplayString);
        VisualElement inputActionInfoUi = CreateInputActionInfoUi(inputActionInfo);
        targetVisualElement.Add(inputActionInfoUi);
        return inputActionInfoUi;
    }

    private static string GetBindingDisplayString(InputAction inputAction, InputDevice inputDevice)
    {
        try
        {
            string combinedBindingDisplayStrings = inputAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice);
            if (combinedBindingDisplayStrings.IsNullOrEmpty())
            {
                return "";
            }

            string[] bindingDisplayStrings = combinedBindingDisplayStrings.Split(InputActionInfo.InfoSeparator);
            string displayStringDeviceText = GetDisplayStringDeviceText(inputDevice);
            if (displayStringDeviceText.IsNullOrEmpty())
            {
                return "";
            }
            string matchingBindingDisplayString = bindingDisplayStrings
                .FirstOrDefault(it => it.Contains(displayStringDeviceText));
            if (matchingBindingDisplayString.IsNullOrEmpty())
            {
                return "";
            }

            return matchingBindingDisplayString
                .Replace(displayStringDeviceText, "")
                .Replace(" +", "+")
                .Replace("+ ", "+")
                .Trim();
        }
        catch (NotImplementedException e)
        {
            Log.Logger.Error(e, $"Could not determine BindingDisplayString for InputAction '{inputAction.name}'");
            return "";
        }
    }

    private static VisualElement CreateInputActionInfoUi(InputActionInfo entry)
    {
        Label label = new Label();
        label.AddToClassList("inputLegendLabel");
        label.text = $"{entry.InputText}: {entry.ActionText}";
        return label;
    }

    private static string GetDisplayStringDeviceText(InputDevice inputDevice)
    {
        if (Gamepad.current != null
            && inputDevice == Gamepad.current)
        {
            return $"[{Gamepad.current.displayName}]";
        }

        if (Keyboard.current != null
                 && inputDevice == Keyboard.current)
        {
            return $"[{Keyboard.current.displayName}]";
        }

        if (Mouse.current != null
                 && inputDevice == Keyboard.current)
        {
            return $"[{Mouse.current.displayName}]";
        }

        if (Touchscreen.current != null
                 && inputDevice == Touchscreen.current)
        {
            return $"[{Touchscreen.current.displayName}]";
        }

        return "";
    }
}
