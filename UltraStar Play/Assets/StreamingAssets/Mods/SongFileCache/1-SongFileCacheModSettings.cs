using System.Collections.Generic;

public class SongFileCacheModSettings : IModSettings
{
    public string songFolder = "";
    public bool cacheFileContent;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => songFolder, newValue => songFolder = newValue) { Label = "Song folder" },
            new BoolModSettingControl(() => cacheFileContent, newValue => cacheFileContent = newValue) { Label = "Cache file content" },
        };
    }
}
