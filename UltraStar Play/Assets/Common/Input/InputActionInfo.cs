using System;
using UnityEngine;

public class InputActionInfo
{
    public const string InfoSeparator = " | ";

    /**
     * Button, gesture, shortcut or description of input to be performed.
     * For example "Ctrl+C".
     */
    public string InputText { get; private set; }

    /**
     * Description of action that will be performed when the input is received.
     * For example "Copy selection".
     */
    public string ActionText { get; private set; }

    /**
     * Sprite that illustrates the input.
     * For example an image of an Escape button, controller button, or touch gesture.
     */
    public Sprite InputSprite { get; private set; }
    
    public InputActionInfo(string actionText, string inputText, Sprite inputSprite = null)
    {
        ActionText = actionText;
        InputText = inputText;
        InputSprite = inputSprite;
    }
}
