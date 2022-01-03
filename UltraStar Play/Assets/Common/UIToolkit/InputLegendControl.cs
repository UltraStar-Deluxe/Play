using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputLegendControl : INeedInjection, IInjectionFinishedListener
{
    private static HashSet<string> inputActionWithDisplayStringError = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        inputActionWithDisplayStringError = new HashSet<string>();
    }

    public EInputSource CurrentInputSource { get; private set; }

    private Dictionary<EInputSource, List<InputActionInfo>> inputLegend = new Dictionary<EInputSource, List<InputActionInfo>>();

    [Inject(UxmlName = R.UxmlNames.inputLegend, Optional = true)]
    private VisualElement inputLegendContainer;

    public void OnInjectionFinished()
    {
        UpdateCurrentInputSource();
    }

    public void AddInputActionInfosForAllDevices(string inputActionPath, string actionText)
    {
        foreach (EInputSource inputSource in EnumUtils.GetValuesAsList<EInputSource>())
        {
            AddInputActionInfo(inputActionPath, actionText, inputSource);
        }
    }

    public void RemoveInputActionInfosForAllDevices(string inputActionPath)
    {
        foreach (EInputSource inputSource in EnumUtils.GetValuesAsList<EInputSource>())
        {
            RemoveInputActionInfo(inputActionPath, inputSource);
        }
    }

    private void RemoveInputActionInfo(string inputActionPath, EInputSource inputSource)
    {
        InputDevice inputDevice = GetInputDevice(inputSource);
        if (inputDevice == null)
        {
            return;
        }

        InputAction inputAction = InputManager.GetInputAction(inputActionPath).InputAction;
        string bindingDisplayString = GetBindingDisplayString(inputAction, inputDevice);
        if (bindingDisplayString.IsNullOrEmpty())
        {
            return;
        }

        if (!inputLegend.TryGetValue(inputSource, out List<InputActionInfo> inputActionInfos))
        {
            return;
        }
        List<InputActionInfo> matchingInputActionInfos = inputActionInfos
            .Where(inputActionInfo => inputActionInfo.InputText == bindingDisplayString)
            .ToList();
        matchingInputActionInfos.ForEach(it => RemoveInputActionInfo(inputSource, it));
    }

    private void AddInputActionInfo(string inputActionPath, string actionText, EInputSource inputSource)
    {
        InputDevice inputDevice = GetInputDevice(inputSource);
        if (inputDevice == null)
        {
            return;
        }

        InputAction inputAction = InputManager.GetInputAction(inputActionPath).InputAction;
        string bindingDisplayString = GetBindingDisplayString(inputAction, inputDevice);
        if (bindingDisplayString.IsNullOrEmpty())
        {
            return;
        }

        AddInputActionInfo(inputSource, new InputActionInfo(actionText, bindingDisplayString));
    }

    private string GetBindingDisplayString(InputAction inputAction, InputDevice inputDevice)
    {
        // if (inputAction.name == "submit")
        // {
        //     if (inputDevice == Keyboard.current)
        //     {
        //         return "Enter";
        //     }
        //     else if (inputDevice == Gamepad.current)
        //     {
        //         return "A";
        //     }
        // }

        if (inputActionWithDisplayStringError.Contains(inputAction.name))
        {
            // Do not try already failed InputActions again.
            return "";
        }

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
            inputActionWithDisplayStringError.Add(inputAction.name);
            return "";
        }
    }

    public void AddInputActionInfo(EInputSource inputSource, InputActionInfo inputActionInfo)
    {
        if (!inputLegend.TryGetValue(inputSource, out List<InputActionInfo> inputActionInfos))
        {
            inputActionInfos = new List<InputActionInfo>();
            inputLegend[inputSource] = inputActionInfos;
        }

        inputActionInfos.Add(inputActionInfo);
        if (inputSource == CurrentInputSource)
        {
            UpdateInputLegendUi();
        }
    }

    public void RemoveInputActionInfo(EInputSource inputSource, InputActionInfo inputActionInfo)
    {
        if (!inputLegend.TryGetValue(inputSource, out List<InputActionInfo> inputLegendEntries))
        {
            return;
        }

        inputLegendEntries.Remove(inputActionInfo);
        if (inputSource == CurrentInputSource)
        {
            UpdateInputLegendUi();
        }
    }

    private void UpdateInputLegendUi()
    {
        if (inputLegendContainer == null)
        {
            return;
        }

        inputLegendContainer.Clear();
        if (inputLegend.TryGetValue(CurrentInputSource, out List<InputActionInfo> inputLegendEntries))
        {
            inputLegendEntries.ForEach(entry => CreateInputActionInfoUi(entry));
        }
    }

    private void CreateInputActionInfoUi(InputActionInfo entry)
    {
        Label label = new Label();
        label.AddToClassList("inputLegendLabel");
        label.text = $"{entry.InputText}: {entry.ActionText}";
        inputLegendContainer.Add(label);
    }

    private EInputSource GetInputSource()
    {
        if (Gamepad.current != null)
        {
            return EInputSource.Gamepad;
        }

        if (Keyboard.current != null)
        {
            return EInputSource.KeyboardAndMouse;
        }

        return EInputSource.Touch;
    }

    private InputDevice GetInputDevice(EInputSource inputSource)
    {
        switch (inputSource)
        {
            case EInputSource.KeyboardAndMouse:
                return Keyboard.current;
            case EInputSource.Gamepad:
                return Gamepad.current;
            case EInputSource.Touch:
                return Touchscreen.current;
            default:
                throw new ArgumentException($"No device defined for inputSource {inputSource}");
        }
    }

    public void UpdateCurrentInputSource()
    {
        CurrentInputSource = GetInputSource();
        UpdateInputLegendUi();
    }

    private string GetDisplayStringDeviceText(InputDevice inputDevice)
    {
        if (Gamepad.current != null
            && inputDevice == Gamepad.current)
        {
            return $"[{Gamepad.current.displayName}]";
        }
        else if (Keyboard.current != null
                 && inputDevice == Keyboard.current)
        {
            return $"[{Keyboard.current.displayName}]";
        }
        else if (Mouse.current != null
                 && inputDevice == Keyboard.current)
        {
            return $"[{Mouse.current.displayName}]";
        }
        else if (Touchscreen.current != null
                 && inputDevice == Touchscreen.current)
        {
            return $"[{Touchscreen.current.displayName}]";
        }

        return "";
    }
}
