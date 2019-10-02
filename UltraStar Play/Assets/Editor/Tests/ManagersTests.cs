using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class ManagersTests
{
    [Test]
    public void TestSettingsManagerGetSetting()
    {
        SettingsManager.Reload();
        Assert.IsTrue((bool)SettingsManager.GetSetting(ESetting.FullScreen));
    }

    [Test]
    public void TestSettingsManagerSetSetting()
    {
        SettingsManager.Reload();
        SettingsManager.SetSetting(ESetting.FullScreen, false);
        Assert.IsFalse((bool)SettingsManager.GetSetting(ESetting.FullScreen));
    }
}
