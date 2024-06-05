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
    private const string TestModName = "TESTMOD";

    private static string testModFolder;

    private static List<TestCaseData> ModNames => ModManager.GetModFolders()
        .Select(modFolder => new TestCaseData(ModManager.GetModFolderName(modFolder)).Returns(null))
        .ToList();

    [TearDown]
    public void DeleteTestModFolder()
    {
        if (testModFolder.IsNullOrEmpty()
            || !Directory.Exists(testModFolder))
        {
            return;
        }
        DirectoryUtils.Delete(testModFolder, true);
        testModFolder = "";
    }

    [UnityTest]
    public IEnumerator NewModLoadsSuccessfully()
    {
        LogAssert.ignoreFailingMessages = true;

        testModFolder = ModManager.Instance.CreateModFolderFromTemplate(TestModName);
        Assert.IsNotNull(testModFolder);
        Assert.IsTrue(Directory.Exists(testModFolder));

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(TestModName);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;

        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);
    }

    [UnityTest]
    [TestCaseSource(nameof(ModNames))]
	public IEnumerator ModsLoadSuccessfully(string modName) {
        LogAssert.ignoreFailingMessages = true;

        SettingsManager.Instance.Settings.EnabledMods.Clear();
        SettingsManager.Instance.Settings.EnabledMods.Add(modName);
        ModManager.Instance.LoadAndInstantiateMods();
        yield return null;

        Assert.IsEmpty(ModManager.Instance.FailedToLoadModFolders);
    }
}
