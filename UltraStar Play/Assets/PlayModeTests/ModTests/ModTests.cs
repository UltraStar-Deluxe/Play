using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ModTests : AbstractPlayModeTest
{
    private static string testModFolder;

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
	public IEnumerator ModsLoadSuccessfullyTest() {
        LogAssert.ignoreFailingMessages = true;

        ModManager modManager = ModManager.Instance;

        testModFolder = modManager.CreateModFolderFromTemplate("TESTMOD");
        Assert.IsNotNull(testModFolder);
        Assert.IsTrue(Directory.Exists(testModFolder));

        modManager.LoadAndInstantiateMods(false);
        yield return null;

        Assert.IsEmpty(modManager.FailedToLoadModFolders);
    }
}
