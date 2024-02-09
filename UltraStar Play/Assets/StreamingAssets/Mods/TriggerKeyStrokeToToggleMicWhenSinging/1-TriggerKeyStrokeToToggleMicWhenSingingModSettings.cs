using System.Collections.Generic;

// Add settings to your mod by implementing IModSettings.
// IModSettings extends IAutoBoundMod,
// which makes an object of the type available in other scripts via Inject attribute.
// Mod settings are saved to file when the app is closed.
public class TriggerKeyStrokeToToggleMicWhenSingingModSettings : IModSettings
{
    public bool showNotificationOnTriggerKeyStroke = true;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new BoolModSettingControl(() => showNotificationOnTriggerKeyStroke, newValue => showNotificationOnTriggerKeyStroke = newValue) { Label = "Show notifications" },
        };
    }
}
