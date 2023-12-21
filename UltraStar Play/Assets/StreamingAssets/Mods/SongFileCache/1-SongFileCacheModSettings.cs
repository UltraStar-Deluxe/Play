using System.Collections.Generic;

public class SongFileCacheModSettings : IModSettings
{
    public string songFolder = "";

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => songFolder, newValue => songFolder = newValue) { Label = "Song folder" },
        };
    }
}
