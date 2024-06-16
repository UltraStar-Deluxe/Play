using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class UltraStarDeluxeHighscoreConnectorModSettings : IModSettings
{
    public string dbPath = "";

    public List<IModSettingControl> GetModSettingControls()
    {
        return new List<IModSettingControl>()
        {
            new StringModSettingControl(() => dbPath, newValue => dbPath = newValue) { Label = "Database Path (Ultrastar.db)" },
        };
    }
}