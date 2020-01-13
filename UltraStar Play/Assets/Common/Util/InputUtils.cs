using UnityEngine;

public static class InputUtils
{
    public static EKeyboardModifier GetCurrentKeyboardModifier()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl);
        bool shift = Input.GetKey(KeyCode.LeftShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt);

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
}
