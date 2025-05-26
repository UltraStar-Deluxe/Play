using System.Collections.Generic;

public class JukeboxAndSingModSettings : IModSettings
{
  public bool HideLyrics { get; set; } = true;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
          new BoolModSettingControl(() => HideLyrics, newValue => HideLyrics = newValue) { Label = "Hide Lyrics", },
        };
    }
}
