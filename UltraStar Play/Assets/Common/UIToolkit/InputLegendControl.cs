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

    private void AddInputActionInfo(string inputActionPath, string actionText, EInputSource inputSource)
    {
        InputAction inputAction = InputManager.GetInputAction(inputActionPath).InputAction;
        // TODO: How to handle composites?
        InputBinding inputBinding = inputAction.bindings
            .FirstOrDefault(binding =>
            {
                InputControl inputControl = InputSystem.FindControl(binding.path);
                return inputControl?.device == GetInputDevice(inputSource);
            });

        if (inputBinding.path.IsNullOrEmpty())
        {
            return;
        }

        AddInputActionInfo(inputSource, new InputActionInfo(actionText, inputBinding.ToDisplayString()));
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
}
