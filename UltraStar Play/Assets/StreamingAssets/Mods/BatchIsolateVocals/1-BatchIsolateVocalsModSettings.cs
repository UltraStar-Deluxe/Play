using System;
using System.Collections.Generic;

public class BatchIsolateVocalsModSettings : IModSettings
{
    public Action OnShowBatchIsolateVocalsDialog { get; set; }

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new ButtonModSettingControl("Select songs to isolate vocals", _ => OnShowBatchIsolateVocalsDialog?.Invoke()),
        };
    }
}
