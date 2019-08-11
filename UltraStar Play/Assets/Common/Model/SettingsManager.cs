using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SettingsManager
{
    private static GameSetting s_setting = new GameSetting();

    public static System.Object GetSetting(ESetting key)
    {
        lock (s_setting)
        {
            return s_setting.GetSettingNotNull(key);
        }
    }

    public static void SetSetting(ESetting key, System.Object settingValue)
    {
        if(settingValue == null)
        {
            throw new UnityException("Can not set setting because value is null!");
        }
        lock (s_setting)
        {
            s_setting.SetSetting(key, settingValue);
        }
    }

    public static void Reload()
    {
        lock (s_setting)
        {
            s_setting = new GameSetting();
        }
    }
}
