using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Responsible.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class AbstractPlayModeTest : AbstractResponsibleTest
{
    protected virtual string TestSceneName => "CommonTestScene";

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        LogAssert.ignoreFailingMessages = true;

        SettingsManager.SettingsLoaderSaver = new TestSettingsLoaderSaver();
        StatisticsManager.StatisticsLoaderSaver = new TestStatisticsLoaderSaver();

        yield return LoadTestScene();

        AssertUtils.HasType<TestSettings>(SettingsManager.Instance.Settings);
        ConfigureTestSettings(SettingsManager.Instance.Settings as TestSettings);
        AssertUtils.HasType<TestStatistics>(StatisticsManager.Instance.Statistics);
        ConfigureTestStatistics(StatisticsManager.Instance.Statistics as TestStatistics);

        ConfigureAndPrepareTestSongs(SettingsManager.Instance.Settings);

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();

        Executor = new UnityTestInstructionExecutor();

        yield return new WaitForEndOfFrame();
    }

    protected virtual string GetAbsoluteTestSongFilePath(string songPathRelativeToTestSongFolderInAssets)
    {
        return $"{Application.dataPath}/Editor/Tests/TestSongs/{songPathRelativeToTestSongFolderInAssets}";
    }

    protected virtual void ConfigureAndPrepareTestSongs(Settings settings)
    {
        List<string> relativeSongFilePaths = GetRelativeTestSongFilePaths();
        if (relativeSongFilePaths.IsNullOrEmpty())
        {
            return;
        }

        // Prepare test song folder
        string testSongFolder = ApplicationUtils.GetTemporaryCachePath("TestSongFolder");
        DirectoryUtils.Delete(testSongFolder, true);
        DirectoryUtils.CreateDirectory(testSongFolder);

        // Add test song folder to settings
        settings.SongDirs = new List<string>()
        {
            testSongFolder,
        };

        // Copy test songs to test song folder
        foreach (string relativeSongFilePath in relativeSongFilePaths)
        {
            string absoluteSongFilePath = GetAbsoluteTestSongFilePath(relativeSongFilePath);
            CopyTestSongToTargetFolder(absoluteSongFilePath, testSongFolder);
        }

        Debug.Log($"Configured test song folder: {testSongFolder}");

        // Reload songs
        SongMetaManager.Instance.ReloadSongMetas();
    }

    private void CopyTestSongToTargetFolder(string sourceSongFilePath, string targetFolder)
    {
        string songFileName = Path.GetFileName(sourceSongFilePath);
        string targetSongFilePath = $"{targetFolder}/{songFileName}";
        Debug.Log($"Copy test song '{songFileName}' to '{targetSongFilePath}'");
        FileUtils.Copy(sourceSongFilePath, targetSongFilePath, false);

        // Copy referenced media files if needed
        CopyTestSongMediaFilesToTargetFolder(sourceSongFilePath, targetSongFilePath);
    }

    private void CopyTestSongMediaFilesToTargetFolder(string sourceSongFilePath, string targetSongFilePath)
    {
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(sourceSongFilePath, out List<SongIssue> songIssues);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Audio), targetSongFilePath);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Video), targetSongFilePath);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Cover), targetSongFilePath);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.Background), targetSongFilePath);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.InstrumentalAudio), targetSongFilePath);
        CopyTestSongMediaFileToTargetFolder(SongMetaUtils.GetAbsoluteFilePath(songMeta, songMeta.VocalsAudio), targetSongFilePath);
    }

    private void CopyTestSongMediaFileToTargetFolder(string mediaFilePath, string targetSongFilePath)
    {
        string mediaFileName = Path.GetFileName(mediaFilePath);
        string targetSongFolderPath = Path.GetDirectoryName(targetSongFilePath);
        string targetMediaFilePath = targetSongFolderPath + $"/{mediaFileName}";
        if (File.Exists(mediaFilePath)
            && !File.Exists(targetMediaFilePath))
        {
            Debug.Log($"Copy test song media '{mediaFilePath}' to '{targetMediaFilePath}'");
            FileUtils.Copy(mediaFilePath, targetMediaFilePath, false);
        }
    }

    protected virtual List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>();
    }

    protected virtual void ConfigureTestStatistics(TestStatistics statistics)
    {
    }

    protected virtual void ConfigureTestSettings(TestSettings settings)
    {
    }

    private IEnumerator LoadTestScene()
    {
        if (TestSceneName.IsNullOrEmpty())
        {
            yield break;
        }

        Debug.Log($"Loading test scene {TestSceneName}");
        SceneManager.LoadScene(TestSceneName, LoadSceneMode.Single);
        yield return new WaitUntilWithTimeout(
            "wait until test scene loaded",
            TimeSpan.FromSeconds(10),
            () => SceneManager.GetActiveScene().name == TestSceneName);
    }
}
