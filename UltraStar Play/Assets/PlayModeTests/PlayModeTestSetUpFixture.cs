using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

/**
 * Setup and teardown that is executed before the first test (OneTimeSetUp) and after the last test (OneTimeTearDown) in this assembly.
 */
[SetUpFixture]
public class PlayModeTestSetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Debug.Log($"{this}.{nameof(OneTimeSetUp)}");

        // Prevent failed tests during setup because of LogAssert unexpected log messages of 'error' severity.
        // Therefore, log nothing or warning instead of error.
        // See https://discussions.unity.com/t/logassert-ignorefailingmessages-does-not-work-still-unhandled-log-message/923329/2
        Debug.Log("Setting NetworkManager LogLevel to Nothing.");
        NetworkManagerInitializationTestUtils.InitNetworkManagerSingleton();
        NetworkManager.Singleton.LogLevel = LogLevel.Nothing;
        Debug.Log("Setting ServerSideCompanionClientManager.CustomNetLogger.ErrorToWarning to true.");
        ServerSideCompanionClientManager.CustomNetLogger.ErrorToWarning = true;

        // Use test specific data.
        SettingsManager.SettingsLoaderSaver = new TestSettingsLoaderSaver();
        StatisticsManager.StatisticsLoaderSaver = new TestStatisticsLoaderSaver();
        IMicrophoneAdapter.Instance = new SimulatedMicrophoneAdapter();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Debug.Log($"{this}.{nameof(OneTimeTearDown)}");

        Debug.Log("Setting NetworkManager LogLevel to Normal.");
        NetworkManagerInitializationTestUtils.InitNetworkManagerSingleton();
        NetworkManager.Singleton.LogLevel = LogLevel.Normal;
        Debug.Log("Setting ServerSideCompanionClientManager.CustomNetLogger.ErrorToWarning to false.");
        ServerSideCompanionClientManager.CustomNetLogger.ErrorToWarning = false;

        // Reset test specific data.
        SettingsManager.SettingsLoaderSaver = null;
        StatisticsManager.StatisticsLoaderSaver = null;
        IMicrophoneAdapter.Instance = new PortAudioForUnityMicrophoneAdapter();
    }
}
