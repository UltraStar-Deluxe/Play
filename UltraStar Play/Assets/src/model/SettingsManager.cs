using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager
{
    private static readonly GameSetting s_setting;

    static SettingsManager()
    {
        s_setting = new GameSetting();
    }

    public static System.Object GetSetting(ESetting key)
    {
        lock (s_setting)
        {
            return s_setting.GetSettingNotNull(key);
        }
    }

    public static void SetSetting(ESetting key, System.Object value)
    {
        if(value == null)
        {
            throw new UnityException("Can not set setting because value is null!");
        }
        lock (s_setting)
        {
            s_setting.SetSetting(key, value);
        }
    }
}
