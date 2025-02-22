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
    private static readonly ModName modWithSettings = new ModName("SongFileCache");

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
    public IEnumerator ModShouldHaveSettings() {
        LogAssertUtils.IgnoreFailingMessages();

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(modWithSettings.Value);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;
        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);

        ModFolder modFolderWithSettings = ModManager.GetModFolder(modWithSettings);
        List<IModSettings> modSettings = ModManager.GetModObjects<IModSettings>(modFolderWithSettings);
        Assert.IsNotEmpty(modSettings);
    }

    [UnityTest]
    public IEnumerator NewModLoadsSuccessfully()
    {
        LogAssertUtils.IgnoreFailingMessages();

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
        LogAssertUtils.IgnoreFailingMessages();

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(modName.Value);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;

        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);
    }
}
