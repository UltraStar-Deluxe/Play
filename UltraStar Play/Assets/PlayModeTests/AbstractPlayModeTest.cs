using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static ConditionUtils;

public abstract class AbstractPlayModeTest : AbstractInputSystemTest, INeedInjection
{
    protected virtual string TestSceneName => "CommonTestScene";
    protected virtual string InitialSetUpSceneName => "CommonTestScene";

    private IDisposable sceneInjectionFinishedSubscription;

    [UnitySetUp]
    public IEnumerator UnitySetUp() => UnitySetUpAsync();
    private async Awaitable UnitySetUpAsync()
    {
        await SetUpTestFixtureAsync();
        await LoadTestSceneAsync();
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown() => UnityTearDownAsync();
    private async Awaitable UnityTearDownAsync()
    {
        await TearDownTestFixtureAsync();
    }

    private async Awaitable SetUpTestFixtureAsync()
    {
        Debug.Log($"{this}.{nameof(SetUpTestFixtureAsync)}");

        await LoadInitialSetUpSceneAsync();

        AssertUtils.HasType<TestSettings>(SettingsManager.Instance.Settings);
        ConfigureTestSettings(SettingsManager.Instance.Settings as TestSettings);

        AssertUtils.HasType<TestStatistics>(StatisticsManager.Instance.Statistics);
        ConfigureTestStatistics(StatisticsManager.Instance.Statistics as TestStatistics);

        AssertUtils.HasType<SimulatedMicrophoneAdapter>(IMicrophoneAdapter.Instance);
        ConfigureMicSampleRecorderSimulation();

        ConfigureAndPrepareTestSongs(SettingsManager.Instance.Settings);

        sceneInjectionFinishedSubscription = UltraStarPlaySceneInjectionManager
            .SceneInjectionFinishedEventStream
            .Subscribe(evt =>
            {
                Debug.Log($"Injecting test class {this}");
                try
                {
                    evt.SceneInjector.Inject(this);
                    sceneInjectionFinishedSubscription?.Dispose();
                }
                catch (InjectionException e)
                {
                    // Only log warning for failed injection
                    // because it is expected when loading intermediate scenes before the final test scene.
                    Debug.LogWarning(e.Message);
                }
            });

        InputFixture = new InputTestFixture();
        Keyboard = InputSystem.GetDevice<Keyboard>();
    }

    private async Awaitable TearDownTestFixtureAsync()
    {
        Debug.Log($"{this}.{nameof(TearDownTestFixtureAsync)}");
        sceneInjectionFinishedSubscription?.Dispose();
        await DeleteAllGameObjectsAsync();
    }

    private void ConfigureMicSampleRecorderSimulation()
    {
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
        UltraStarSongMeta songMeta = UltraStarSongParser.ParseFile(sourceSongFilePath).SongMeta;
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

    protected virtual async Awaitable LoadTestSceneAsync()
    {
        if (TestSceneName.IsNullOrEmpty())
        {
            Debug.Log("Skip loading test scene, TestSceneName is null or empty.");
            return;
        }

        await LoadSceneByNameAsync(TestSceneName);
    }

    private async Awaitable LoadInitialSetUpSceneAsync()
    {
        // Start with a simple scene that has all common objects but does not require a special game state.
        await LoadSceneByNameAsync(InitialSetUpSceneName);
    }

    private async Awaitable LoadSceneByNameAsync(string sceneName)
    {
        if (sceneName.IsNullOrEmpty())
        {
            return;
        }

        Debug.Log($"Loading test scene {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        await WaitForConditionAsync(
            () => SceneManager.GetActiveScene().name == sceneName,
            new WaitForConditionConfig {description = $"test scene loaded: sceneName '{sceneName}'"});

        await WaitForConditionAsync(
            () => DontDestroyOnLoadManager.Instance != null,
            new WaitForConditionConfig { description = "DontDestroyOnLoadManager instance is present" });
    }

    private async Awaitable DeleteAllGameObjectsAsync()
    {
        List<GameObject> gameObjects = new List<GameObject>();

        // Destroy regular objects in scene
        foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObjects.Add(gameObject);
        }

        // Destroy DontDestroyOnLoad objects
        if (DontDestroyOnLoadManager.Instance != null)
        {
            gameObjects.Add(DontDestroyOnLoadManager.Instance.gameObject);
        }

        foreach (GameObject gameObject in gameObjects)
        {
            GameObject.Destroy(gameObject);
        }

        // Wait for objects to be destroyed
        await WaitForConditionAsync(() =>
        {
            Debug.Log("Waiting for GameObjects to be destroyed.");
            return gameObjects.AllMatch(destroyedGameObject => destroyedGameObject == null);
        });
        Debug.Log("All GameObjects have been destroyed.");

        await Awaitable.NextFrameAsync();
    }
}
