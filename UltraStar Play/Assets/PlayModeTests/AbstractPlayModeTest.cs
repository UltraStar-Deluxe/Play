using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Responsible.Unity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class AbstractPlayModeTest : AbstractResponsibleTest, INeedInjection
{
    protected virtual string TestSceneName => "CommonTestScene";

    private IDisposable sceneInjectionFinishedSubscription;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        LogAssert.ignoreFailingMessages = true;

        yield return SetUpTestFixture();

        yield return LoadTestScene();
        yield return new WaitForEndOfFrame();
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        LogAssert.ignoreFailingMessages = true;
        yield return TearDownTestFixture();
    }

    private IEnumerator SetUpTestFixture()
    {
        SettingsManager.SettingsLoaderSaver = new TestSettingsLoaderSaver();
        StatisticsManager.StatisticsLoaderSaver = new TestStatisticsLoaderSaver();
        IMicrophoneAdapter.Instance = new SimulatedMicrophoneAdapter();

        sceneInjectionFinishedSubscription = UltraStarPlaySceneInjectionManager
            .SceneInjectionFinishedEventStream
            .Subscribe(evt => evt.SceneInjector.Inject(this));

        yield return LoadInitialScene();

        AssertUtils.HasType<TestSettings>(SettingsManager.Instance.Settings);
        ConfigureTestSettings(SettingsManager.Instance.Settings as TestSettings);

        AssertUtils.HasType<TestStatistics>(StatisticsManager.Instance.Statistics);
        ConfigureTestStatistics(StatisticsManager.Instance.Statistics as TestStatistics);

        AssertMicSampleRecorderIsSimulated();

        ConfigureAndPrepareTestSongs(SettingsManager.Instance.Settings);

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();

        Executor = new UnityTestInstructionExecutor();
    }

    private IEnumerator TearDownTestFixture()
    {
        SettingsManager.SettingsLoaderSaver = null;
        StatisticsManager.StatisticsLoaderSaver = null;
        IMicrophoneAdapter.Instance = new PortAudioForUnityMicrophoneAdapter();
        sceneInjectionFinishedSubscription?.Dispose();
        DeleteAllGameObjects();
        yield return null;
    }

    private void AssertMicSampleRecorderIsSimulated()
    {
        AssertUtils.HasType<SimulatedMicrophoneAdapter>(IMicrophoneAdapter.Instance);

        string simulatedMicName = IMicrophoneAdapter.Instance.Devices.FirstOrDefault();
        MicProfile simulatedMicProfile = new MicProfile(simulatedMicName);
        MicSampleRecorder simulatedMicSampleRecorder = MicSampleRecorderManager.Instance.GetOrCreateMicSampleRecorder(simulatedMicProfile);
        simulatedMicSampleRecorder.StartRecording();
        Assert.IsTrue(simulatedMicSampleRecorder.IsRecording.Value, "Microphone simulation not set up correctly. Should be recording.");
        simulatedMicSampleRecorder.StopRecording();
        Assert.IsFalse(simulatedMicSampleRecorder.IsRecording.Value, "Microphone simulation not set up correctly. Should not be recording.");
    }

    protected static string GetAbsoluteTestSongFilePath(string songPathRelativeToTestSongFolderInAssets)
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
        SongMetaManager.Instance.RescanSongs();
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
        return new List<string>() { "SingingTestSongs/ThreeQuartersA4OneQuarterC5.txt" };
    }

    protected virtual void ConfigureTestStatistics(TestStatistics statistics)
    {
    }

    protected virtual void ConfigureTestSettings(TestSettings settings)
    {
        PlayerProfile playerProfile = new PlayerProfile("TestPlayer1", EDifficulty.Medium);
        settings.PlayerProfiles = new List<PlayerProfile>()
        {
            playerProfile,
        };

        MicProfile micProfile = new MicProfile("TestMic1");
        settings.MicProfiles = new List<MicProfile>()
        {
            micProfile,
        };

        // Song select should automatically assign the last used mic to the player.
        settings.PlayerProfileNameToLastUsedMicProfile.Add(playerProfile.Name, new MicProfileReference(micProfile));

        // Simulate connected mic with A4 pitch frequency
        SimulatedMicrophoneAdapter.SetSimulatedDevices(new List<string>()
        {
            micProfile.Name,
        });
        SimulatedMicrophoneAdapter.SetSimulatedDevicePitchInHz(playerProfile.Name, 440);
    }

    private IEnumerator LoadInitialScene()
    {
        // Start with a simple scene that has all common objects but does not require a special game state.
        yield return LoadSceneByName("CommonTestScene");
    }

    private IEnumerator LoadTestScene()
    {
        yield return LoadSceneByName(TestSceneName);
    }

    private static IEnumerator LoadSceneByName(string sceneName)
    {
        if (sceneName.IsNullOrEmpty())
        {
            yield break;
        }

        Debug.Log($"Loading test scene {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        yield return new WaitUntilWithTimeout(
            $"Wait until test scene loaded: sceneName '{sceneName}'",
            TimeSpan.FromSeconds(10),
            () => SceneManager.GetActiveScene().name == sceneName);
    }

    private void DeleteAllGameObjects()
    {
        foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            GameObject.Destroy(gameObject);
        }
        GameObject.Destroy(DontDestroyOnLoadManager.Instance.gameObject);
    }
}
