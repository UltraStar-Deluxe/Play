using System;
using System.Collections.Generic;
using UniInject;
using WindowsInput.Native;

public class TriggerKeyStrokeToToggleMicWhenSingingModSettings : IModSettings
{
    public VirtualKeyCode keyCode = VirtualKeyCode.F9;
    public bool requireControlModifier = true;
    public bool showNotificationOnTriggerKeyStroke = true;
    
    public Action OnTriggerShortcut { get; set; }

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new BoolModSettingControl(() => showNotificationOnTriggerKeyStroke, newValue => showNotificationOnTriggerKeyStroke = newValue) { Label = "Show notifications" },
            new BoolModSettingControl(() => requireControlModifier, newValue => requireControlModifier = newValue) { Label = "Required pressed Control / Ctrl" },
            new EnumModSettingControl<VirtualKeyCode>(() => keyCode, newValue => keyCode = newValue) { Label = "KeyCode" },
            new ButtonModSettingControl("Test Shortcut", evt => OnTriggerShortcut?.Invoke()),
        };
    }
}
