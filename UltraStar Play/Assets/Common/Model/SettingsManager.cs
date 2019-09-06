using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SettingsManager
{
    private static GameSetting setting = new GameSetting();

    public static object GetSetting(ESetting key)
    {
        lock (setting)
        {
            return setting.GetSettingNotNull(key);
        }
    }

    public static void SetSetting(ESetting key, object settingValue)
    {
        if (settingValue == null)
        {
            throw new UnityException("Cannot set setting because value is null");
        }
        lock (setting)
        {
            setting.SetSetting(key, settingValue);
        }
    }

    public static void Reload()
    {
        lock (setting)
        {
            setting = new GameSetting();
        }
    }
}
