using System.Collections.Generic;

public class LocalFolderSongRepositoryModSettings : IModSettings
{
    public string songFolder;
    public string encodingName;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => songFolder, newValue => songFolder = newValue) { Label = "Song Folder", },
            new StringModSettingControl(() => encodingName, newValue => encodingName = newValue) { Label = "Encoding (optional, UTF-8, ISO-8859-1, etc.)", }
        };
    }
}