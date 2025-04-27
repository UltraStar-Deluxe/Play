using System;
using System.Collections.Generic;
using WindowsInput.Events;
using Newtonsoft.Json;

public class TriggerKeyStrokeToToggleMicWhenSingingModSettings : IModSettings
{
    public KeyCode keyCode = KeyCode.F9;
    public bool requireControlModifier = true;
    public bool showNotificationOnTriggerKeyStroke = true;

    [JsonIgnore]
    public Action OnTriggerShortcut { get; set; }

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new BoolModSettingControl(() => showNotificationOnTriggerKeyStroke, newValue => showNotificationOnTriggerKeyStroke = newValue) { Label = "Show notifications" },
            new BoolModSettingControl(() => requireControlModifier, newValue => requireControlModifier = newValue) { Label = "Required pressed Control / Ctrl" },
            new EnumModSettingControl<KeyCode>(() => keyCode, newValue => keyCode = newValue) { Label = "KeyCode" },
            new ButtonModSettingControl("Test Shortcut", evt => OnTriggerShortcut?.Invoke()),
        };
    }
}
