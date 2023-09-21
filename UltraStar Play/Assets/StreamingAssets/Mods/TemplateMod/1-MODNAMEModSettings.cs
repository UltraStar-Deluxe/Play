using System.Collections.Generic;

// Add settings to your mod by implementing IModSettings.
// IModSettings extends IAutoBoundMod,
// which makes an object of the type available in other scripts via Inject attribute.
// Mod settings are saved to file when the app is closed.
public class MODNAMEModSettings : IModSettings
{
    public bool myBool = true;
    public double myDouble = 12.34;
    public int myInt = 42;
    public string myString = "text";

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new BoolModSettingControl(() => myBool, newValue => myBool = newValue) { Label = "My Bool" },
            new DoubleModSettingControl(() => myDouble, newValue => myDouble = newValue) { Label = "My Double" },
            new IntModSettingControl(() => myInt, newValue => myInt = newValue) { Label = "My Int" },
            new StringModSettingControl(() => myString, newValue => myString = newValue) { Label = "My String" },
        };
    }
}
