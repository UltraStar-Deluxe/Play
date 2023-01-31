using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using SimpleHttpServerForUnity;
using UnityEngine;
using UniInject;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class InputSimulatorRestControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HttpServer httpServer;

    private Keyboard currentKeyboard;

    private ConcurrentBag<KeyControl> keyControlsToBeTriggered = new();
    private List<KeyControl> triggeredKeyControlsOfLastFrame = new();

	private void Start()
    {
        currentKeyboard = Keyboard.current;

        RegisterNavigationEndpoint("left",
            "Simulate left arrow key press",
            () => currentKeyboard.leftArrowKey);

        RegisterNavigationEndpoint("right",
            "Simulate right arrow key press",
            () => currentKeyboard.rightArrowKey);

        RegisterNavigationEndpoint("up",
            "Simulate up arrow key press",
            () => currentKeyboard.upArrowKey);

        RegisterNavigationEndpoint("down",
            "Simulate down arrow key press",
            () => currentKeyboard.downArrowKey);

        RegisterNavigationEndpoint("enter",
            "Simulate enter key press",
            () => currentKeyboard.enterKey);

        RegisterNavigationEndpoint("escape",
            "Simulate escape key press",
            () => currentKeyboard.escapeKey);

        RegisterNavigationEndpoint("space",
            "Simulate space key press",
            () => currentKeyboard.spaceKey);
	}

    private void Update()
    {
        if (keyControlsToBeTriggered.IsEmpty
            && triggeredKeyControlsOfLastFrame.IsNullOrEmpty())
        {
            return;
        }

        if (currentKeyboard == null)
        {
            Debug.LogWarning($"Cannot simulate input events because no keyboard was found");
        }
        else
        {
            using (StateEvent.From(currentKeyboard, out InputEventPtr inputEventPtr))
            {
                triggeredKeyControlsOfLastFrame.ForEach(keyControl =>
                {
                    SetKeyboardButtonEvent(keyControl, inputEventPtr, 0);
                });
                triggeredKeyControlsOfLastFrame = new();

                keyControlsToBeTriggered.ForEach(keyControl =>
                {
                    SetKeyboardButtonEvent(keyControl, inputEventPtr, 1);
                    triggeredKeyControlsOfLastFrame.Add(keyControl);
                });
            }
        }
        keyControlsToBeTriggered = new();
    }

    private void RegisterNavigationEndpoint(string keyboardButton, string description, Func<KeyControl> keyControlGetter)
    {
        string path = $"api/rest/input/{keyboardButton}";
        httpServer.On(HttpMethod.Post, path)
            .WithDescription(description)
            .UntilDestroy(gameObject)
            .Do(_ =>
            {
                Debug.Log($"Received input simulation request {path}");
                KeyControl keyControl = keyControlGetter();
                keyControlsToBeTriggered.Add(keyControl);
            });
    }

    private void SetKeyboardButtonEvent(KeyControl keyControl, InputEventPtr eventPtr, float value)
    {
        Debug.Log($"Simulating keyboard button event {value} for {keyControl}");
        keyControl.WriteValueIntoEvent(value, eventPtr);
        InputSystem.QueueEvent(eventPtr);
    }
}
