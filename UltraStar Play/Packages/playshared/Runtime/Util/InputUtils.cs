using PrimeInputActions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public static class InputUtils
{
    public const float DoubleClickThresholdInSeconds = 0.3f;
    public const float DragDistanceThresholdInPx = 5f;

    public static EKeyboardModifier GetCurrentKeyboardModifier(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return EKeyboardModifier.None;
        }
        
        bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;

        if (ctrl && !shift && !alt)
        {
            return EKeyboardModifier.Ctrl;
        }
        else if (!ctrl && shift && !alt)
        {
            return EKeyboardModifier.Shift;
        }
        else if (!ctrl && !shift && alt)
        {
            return EKeyboardModifier.Alt;
        }
        else if (ctrl && shift && !alt)
        {
            return EKeyboardModifier.CtrlShift;
        }
        else if (ctrl && !shift && alt)
        {
            return EKeyboardModifier.CtrlAlt;
        }
        else if (!ctrl && shift && alt)
        {
            return EKeyboardModifier.ShiftAlt;
        }
        else if (ctrl && shift && alt)
        {
            return EKeyboardModifier.CtrlShiftAlt;
        }
        return EKeyboardModifier.None;
    }

    public static bool AnyKeyboardModifierPressed()
    {
        return InputManager.GetInputAction("usplay/anyKeyboardModifier").ReadValue<float>() > 0;
    }

    public static bool AnyKeyboardOrMouseOrTouchPressed(Keyboard keyboard, Mouse mouse)
    {
        return AnyKeyboardButtonPressed(keyboard)
               || AnyMouseButtonPressed(mouse)
               || AnyTouchscreenPressed();
    }
    
    public static bool AnyKeyboardButtonPressed(Keyboard keyboard)
    {
        return keyboard != null
               && keyboard.anyKey.ReadValue() > 0;
    }

    public static bool AnyMouseButtonPressed(Mouse mouse)
    {
        return mouse != null
               && (mouse.leftButton.isPressed
                   || mouse.rightButton.isPressed
                   || mouse.middleButton.isPressed);
    }
    
    public static bool AnyTouchscreenPressed()
    {
        return Touch.activeTouches.Count > 0;
    }

    public static bool IsKeyboardShiftPressed(Keyboard keyboard)
    {
        return keyboard != null
               && (keyboard.leftShiftKey.isPressed
                   || keyboard.rightShiftKey.isPressed);
    }
    
    public static bool IsKeyboardControlPressed(Keyboard keyboard)
    {
        return keyboard != null
               && (keyboard.leftCtrlKey.isPressed
                   || keyboard.rightCtrlKey.isPressed);
    }

    public static bool IsAnyKeyboardModifierPressed(Keyboard keyboard)
    {
        return IsKeyboardShiftPressed(keyboard)
               || IsKeyboardControlPressed(keyboard)
               || IsKeyboardAltPressed(keyboard);
    }

    
    public static bool IsKeyboardAltPressed(Keyboard keyboard)
    {
        return keyboard != null
               && (keyboard.leftAltKey.isPressed
                   || keyboard.rightAltKey.isPressed);
    }

    public static bool WasPressedOrReleasedInThisFrame(ButtonControl buttonControl)
    {
        return buttonControl.wasPressedThisFrame || buttonControl.wasReleasedThisFrame;
    }

    public static Vector2 GetMousePosition(Mouse mouse)
    {
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
    }

    public static Vector2 GetPointerPositionInPanelCoordinates(Pointer pointer, PanelHelper panelHelper, bool invertY = false)
    {
        if (pointer == null)
        {
            return Vector2.zero;
        }

        Vector2 pointerScreenCoordinates = new(pointer.position.x.ReadValue(), pointer.position.y.ReadValue());
        Vector2 pointerPanelCoordinates = panelHelper.ScreenToPanel(pointerScreenCoordinates);
        if (invertY)
        {
            Vector2 screenSizeInPanelCoordinates = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
            return new Vector2(pointerPanelCoordinates.x, screenSizeInPanelCoordinates.y - pointerPanelCoordinates.y);
        }

        return pointerPanelCoordinates;
    }

    public static bool IsPointerOverVisualElement(Pointer pointer, VisualElement visualElement, PanelHelper panelHelper)
    {
        if (pointer == null)
        {
            return false;
        }

        Vector2 pointerPositionInPanelCoordinates = GetPointerPositionInPanelCoordinates(pointer, panelHelper, true);
        pointerPositionInPanelCoordinates = new Vector2(pointerPositionInPanelCoordinates.x,
            pointerPositionInPanelCoordinates.y);
        Rect rect = visualElement.worldBound;
        return rect.xMin <= pointerPositionInPanelCoordinates.x
               && pointerPositionInPanelCoordinates.x <= rect.xMax
               && rect.yMin <= pointerPositionInPanelCoordinates.y
               && pointerPositionInPanelCoordinates.y <= rect.yMax;
    }
}
