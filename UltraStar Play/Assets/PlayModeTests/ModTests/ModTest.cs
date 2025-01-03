using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ModTest : AbstractPlayModeTest
{
    private static readonly ModName testModName = new ModName("TESTMOD");

    private static ModFolder testModFolder;

    private static List<TestCaseData> ModNames => ModManager.GetModFolders()
        .Select(modFolder => new TestCaseData(modFolder.ModName).Returns(null))
        .ToList();

    [TearDown]
    public void DeleteTestModFolder()
    {
        if (testModFolder == null
            || !Directory.Exists(testModFolder.Value))
        {
            return;
        }
        DirectoryUtils.Delete(testModFolder.Value, true);
        testModFolder = null;
    }

    [UnityTest]
    public IEnumerator NewModLoadsSuccessfully()
    {
        LogAssert.ignoreFailingMessages = true;

        testModFolder = ModManager.Instance.CreateModFolderFromTemplate(testModName);
        Assert.IsNotNull(testModFolder);
        Assert.IsTrue(Directory.Exists(testModFolder.Value));

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(testModName.Value);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;

        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);
    }

    [UnityTest]
    [TestCaseSource(nameof(ModNames))]
	public IEnumerator ModsLoadSuccessfully(ModName modName) {
        LogAssert.ignoreFailingMessages = true;

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(modName.Value);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;

        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);
    }
}
