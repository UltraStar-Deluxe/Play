using System;
using System.Collections.Generic;
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

    public static Vector2 GetArrowKeyDirection()
    {
        Vector2 result = Vector2.zero;
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            result += new Vector2(-1, 0);
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            result += new Vector2(1, 0);
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            result += new Vector2(0, 1);
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            result += new Vector2(0, -1);
        }
        return result;
    }

    // Alternative implementation of Input.inputString.
    // Input.inputString does not work when combined with certain modifier keys (e.g. Ctrl and Alt).
    public static string GetTypedLetter()
    {
        if (!Input.anyKey)
        {
            return "";
        }

        if (GetCurrentKeyboardModifier() == EKeyboardModifier.None)
        {
            return Input.inputString;
        }

        // Search in down keys for letters.
        List<KeyCode> keyCodes = GetCurrentKeyDownKeyCodes();
        if (keyCodes.Count != 1)
        {
            return "";
        }
        string typedLetter = GetCharacter(keyCodes[0]);
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            typedLetter = typedLetter.ToUpperInvariant();
        }
        return typedLetter;
    }

    public static List<KeyCode> GetCurrentKeyDownKeyCodes()
    {
        List<KeyCode> result = new List<KeyCode>();
        foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(vKey))
            {
                result.Add(vKey);
            }
        }
        return result;
    }

    public static string GetCharacter(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Plus:
            case KeyCode.KeypadPlus: return "+";
            case KeyCode.Minus:
            case KeyCode.KeypadMinus: return "-";
            case KeyCode.Alpha0:
            case KeyCode.Keypad0: return "0";
            case KeyCode.Alpha1:
            case KeyCode.Keypad1: return "1";
            case KeyCode.Alpha2:
            case KeyCode.Keypad2: return "2";
            case KeyCode.Alpha3:
            case KeyCode.Keypad3: return "3";
            case KeyCode.Alpha4:
            case KeyCode.Keypad4: return "4";
            case KeyCode.Alpha5:
            case KeyCode.Keypad5: return "5";
            case KeyCode.Alpha6:
            case KeyCode.Keypad6: return "6";
            case KeyCode.Alpha7:
            case KeyCode.Keypad7: return "7";
            case KeyCode.Alpha8:
            case KeyCode.Keypad8: return "8";
            case KeyCode.Alpha9:
            case KeyCode.Keypad9: return "9";
            case KeyCode.Exclaim: return "!";
            case KeyCode.Question: return "?";
            case KeyCode.DoubleQuote: return "\"";
            case KeyCode.Hash: return "#";
            case KeyCode.Dollar: return "$";
            case KeyCode.Slash:
            case KeyCode.KeypadDivide: return "/";
            case KeyCode.Backslash: return "\\";
            case KeyCode.Space: return " ";
            case KeyCode.Percent: return "%";
            case KeyCode.Ampersand: return "&";
            case KeyCode.Quote: return "'";
            case KeyCode.LeftParen: return "(";
            case KeyCode.RightParen: return ")";
            case KeyCode.Asterisk:
            case KeyCode.KeypadMultiply: return "*";
            case KeyCode.Comma: return ",";
            case KeyCode.Period:
            case KeyCode.KeypadPeriod: return ".";
            case KeyCode.Colon: return ":";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Underscore: return "_";
            case KeyCode.Less: return "<";
            case KeyCode.Greater: return ">";
            case KeyCode.Equals: return "=";
            case KeyCode.At: return "@";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.LeftCurlyBracket: return "{";
            case KeyCode.RightCurlyBracket: return "}";

            default: return (keyCode.ToString().Length == 1) ? keyCode.ToString() : "";
        }
    }
}
