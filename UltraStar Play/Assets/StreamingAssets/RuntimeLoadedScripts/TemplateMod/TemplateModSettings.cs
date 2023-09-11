// Add settings to your mod by implementing IModSettings.
// IModSettings extends IAutoBoundRuntimeLoadedScript,
// which makes an object of the type available in other scripts via Inject attribute.
public class MODNAMEModSettings : IModSettings
{
    public bool myBool = true;
    public double myDouble = 12.34;
    public int myInt = 42;
    public string myString = "some text";

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new BoolModSettingControl(() => myBool, newValue => myBool = newValue) { Label = "My Bool" },
            new IntModSettingControl(() => myDouble, newValue => myDouble = newValue) { Label = "My Double" },
            new DoubleModSettingControl(() => myInt, newValue => myInt = newValue) { Label = "My Int" },
            new StringModSettingControl(() => myString, newValue => myString = newValue) { Label = "My String" },
        };
    }
}