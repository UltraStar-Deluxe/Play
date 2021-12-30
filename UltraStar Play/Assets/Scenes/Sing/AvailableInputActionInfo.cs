using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AvailableInputActionInfo : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Text uiText;
    
    public bool isInitialized;

    [TextArea(3, 8)]
    public string inputActionInfo;
    
    private readonly List<string> ignoredInputActions = new List<string> {
        "navigate",
        "scrollWheel",
        "middleClick",
        "rightClick",
        "trackedDevicePosition",
        "trackedDeviceOrientation",
        "anyKeyboardModifier",
        "space",
        "enter",
        "closeContextMenu",
    };

    private void OnEnable()
    {
        isInitialized = false;
        inputActionInfo = "";
    }

    private void Update()
	{
        if (!isInitialized)
        {
            isInitialized = true;
            List<string> availableInputActionInfosRichText = GetAvailableInputActionInfos()
                .Select(it =>
                {
                    string actionNameDisplayString = CamelCaseToDisplayName(it.ActionText.Replace(" ", ""));
                    return $"<color=\"orange\">{actionNameDisplayString}:</color> {it.InputText}";
                }).ToList();
            inputActionInfo = string.Join("\n\n", availableInputActionInfosRichText);
            if (uiText != null)
            {
                // Additional line break for better scrolling
                uiText.text = inputActionInfo + "\n";
            }
        }
	}

    private List<InputActionInfo> GetAvailableInputActionInfos()
    {
        List<InputActionInfo> infos = new List<InputActionInfo>();
        foreach (ObservableCancelablePriorityInputAction observableInputAction in InputManager.GetInputActionsWithSubscribers())
        {
            if (ignoredInputActions.Contains(observableInputAction.InputAction.name))
            {
                continue;
            }

            string displayString = GetBindingDisplayString(observableInputAction.InputAction);
            if (displayString.IsNullOrEmpty())
            {
                continue;
            }

            string actionNameDisplayString = CamelCaseToDisplayName(observableInputAction.InputAction.name);
            string infoOfThisAction = GetBindingDisplayString(observableInputAction.InputAction);
            infos.Add(new InputActionInfo(actionNameDisplayString, infoOfThisAction));
        }

        foreach (InputActionInfo additionalInputActionInfo in UltraStarPlayInputManager.AdditionalInputActionInfos)
        {
            InputActionInfo existingInfo = infos.FirstOrDefault(it =>
            {
                string actionNameToLowerNoSpace = it.ActionText.ToLowerInvariant().Replace(" ", "");
                string additionalInputActionNameToLowerNoSpace = additionalInputActionInfo.ActionText.ToLowerInvariant().Replace(" ", "");
                return actionNameToLowerNoSpace == additionalInputActionNameToLowerNoSpace;
            });
            if (existingInfo == null)
            {
                infos.Add(additionalInputActionInfo);
            }
            else
            {
                existingInfo.AddInfoText(additionalInputActionInfo.InputText);
            }
        }
        
        infos.Sort(InputActionInfo.CompareByActionName);
        return infos;
    }

    private string CamelCaseToDisplayName(string text)
    {
        string camelCaseWithSpaces = text.SplitCamelCase();
        List<string> words = camelCaseWithSpaces.Split(' ').ToList();
        return string.Join(" ", words.Select(word => word.ToUpperInvariantFirstChar()));
    }

    private static string GetBindingDisplayString(InputAction action)
    {
        StringBuilder sb = new StringBuilder();
        ReadOnlyArray<InputBinding> bindings = action.bindings;
        for (int i = 0; i < bindings.Count; ++i)
        {
            InputBinding binding = bindings[i];
            // Only consider actions that make use of connected devices.
            // And only create info text for a composite but not for its parts.
            if (binding.isPartOfComposite
                || (!binding.isComposite && InputSystem.FindControl(binding.path) == null))
            {
                continue;
            }
            
            string text = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontOmitDevice);
            if (sb.Length > 0)
            {
                sb.Append($" | {text}");
            }
            else
            {
                sb.Append(text);                
            }
        }

        return sb.ToString();
    }
}
