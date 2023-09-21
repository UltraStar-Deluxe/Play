using System.Collections.Generic;

public class LocalFolderSongRepositoryModSettings : IModSettings
{
    public string songFolder;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => songFolder, newValue => songFolder = newValue)
            {
                Label = "Song Folder",
            }
        };
    }
}