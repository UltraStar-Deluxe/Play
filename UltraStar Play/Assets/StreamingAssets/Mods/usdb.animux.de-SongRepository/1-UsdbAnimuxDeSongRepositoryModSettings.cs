using System.Collections.Generic;

public class UsdbAnimuxDeSongSynchronizerModSettings : IModSettings
{
    public string username = "";
    public string password = "";
    public bool loadFullSongIndex;
    public int maxSongIndexCacheAgeInDays = 7;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => username, newValue => username = newValue) { Label="User Name" },
            new StringModSettingControl(() => password, newValue => password = newValue) { Label="Password", IsPassword = true },
            new BoolModSettingControl(() => loadFullSongIndex, newValue => loadFullSongIndex = newValue) { Label="Load full song index into cache" },
            new IntModSettingControl(() => maxSongIndexCacheAgeInDays, newValue => maxSongIndexCacheAgeInDays = newValue) { Label="Max age of song index cache (days)" },
        };
    }
}