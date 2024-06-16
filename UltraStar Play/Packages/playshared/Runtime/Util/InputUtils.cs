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

    private static Keyboard lastNonVirtualKeyboard;
    private static Mouse lastNonVirtualMouse;
    private static Pointer lastNonVirtualPointer;
    
    public static Keyboard GetNonVirtualKeyboard()
    {
        Keyboard current = Keyboard.current;
        if (!current.name.ToLowerInvariant().Contains("virtual"))
        {
            lastNonVirtualKeyboard = current;
        }
        return lastNonVirtualKeyboard;
    }

    public static Mouse GetNonVirtualMouse()
    {
        Mouse current = Mouse.current;
        if (!current.name.ToLowerInvariant().Contains("virtual"))
        {
            lastNonVirtualMouse = current;
        }
        return lastNonVirtualMouse;
    }
    
    public static Pointer GetNonVirtualPointer()
    {
        Pointer current = Pointer.current;
        if (!current.name.ToLowerInvariant().Contains("virtual"))
        {
            lastNonVirtualPointer = current;
        }
        return lastNonVirtualPointer;
    }
    
    public static EKeyboardModifier GetCurrentKeyboardModifier()
    {
        Keyboard keyboard = GetNonVirtualKeyboard();
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

    public static bool AnyKeyboardOrMouseOrTouchPressed()
    {
        return AnyKeyboardButtonPressed()
               || AnyMouseButtonPressed()
               || AnyTouchscreenPressed();
    }
    
    public static bool AnyKeyboardButtonPressed()
    {
        Keyboard keyboard = GetNonVirtualKeyboard();
        return keyboard != null
               && keyboard.anyKey.ReadValue() > 0;
    }

    public static bool AnyMouseButtonPressed()
    {
        Mouse mouse = GetNonVirtualMouse();
        return mouse != null
               && (mouse.leftButton.isPressed
                   || mouse.rightButton.isPressed
                   || mouse.middleButton.isPressed);
    }
    
    public static bool AnyTouchscreenPressed()
    {
        return Touch.activeTouches.Count > 0;
    }

    public static bool IsKeyboardShiftPressed()
    {
        Keyboard keyboard = GetNonVirtualKeyboard();
        return keyboard != null
               && (keyboard.leftShiftKey.isPressed
                   || keyboard.rightShiftKey.isPressed);
    }
    
    public static bool IsKeyboardControlPressed()
    {
        Keyboard keyboard = GetNonVirtualKeyboard();
        return keyboard != null
               && (keyboard.leftCtrlKey.isPressed
                   || keyboard.rightCtrlKey.isPressed);
    }

    public static bool IsAnyKeyboardModifierPressed()
    {
        return IsKeyboardShiftPressed()
               || IsKeyboardControlPressed()
               || IsKeyboardAltPressed();
    }

    public static bool IsKeyboardAltPressed()
    {
        Keyboard keyboard = GetNonVirtualKeyboard();
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
    
    public static Vector2 GetCurrentPointerPosition()
    {
        if (Pointer.current == null)
        {
            return Vector2.zero;
        }
        return Pointer.current.position.value;
    }

    public static Vector2 GetPointerPositionInPanelCoordinates(PanelHelper panelHelper, bool invertY = false)
    {
        Pointer pointer = GetNonVirtualPointer();
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

    public static bool IsPointerOverVisualElement(VisualElement visualElement, PanelHelper panelHelper)
    {
        Pointer pointer = GetNonVirtualPointer();
        if (pointer == null)
        {
            return false;
        }

        Vector2 pointerPositionInPanelCoordinates = GetPointerPositionInPanelCoordinates(panelHelper, true);
        pointerPositionInPanelCoordinates = new Vector2(pointerPositionInPanelCoordinates.x,
            pointerPositionInPanelCoordinates.y);
        Rect rect = visualElement.worldBound;
        return rect.xMin <= pointerPositionInPanelCoordinates.x
               && pointerPositionInPanelCoordinates.x <= rect.xMax
               && rect.yMin <= pointerPositionInPanelCoordinates.y
               && pointerPositionInPanelCoordinates.y <= rect.yMax;
    }

    public static bool IsPointerDown()
    {
        if (Pointer.current == null)
        {
            return false;
        }

        return Pointer.current.press.isPressed;
    }
}
