using System.Collections.Generic;

public class MicRecordingSaverModSettings : IModSettings
{
    public int audioShiftInMillis;

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new IntModSettingControl(() => audioShiftInMillis, newValue => audioShiftInMillis = newValue) { Label = "Audio shift (ms)" },
        };
    }
}
