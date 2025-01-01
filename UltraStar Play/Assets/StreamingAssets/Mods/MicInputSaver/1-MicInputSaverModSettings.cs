using System.Collections.Generic;

public class MicInputSaverModSettings : IModSettings
{
    public string targetDirectory = "";

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => targetDirectory, newValue => targetDirectory = newValue) { Label = "Target directory (leave empty for default)" },
        };
    }
}
