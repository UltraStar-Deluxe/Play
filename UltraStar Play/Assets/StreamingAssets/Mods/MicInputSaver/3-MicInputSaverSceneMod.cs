using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class MicInputSaverSceneMod : ISceneMod
{
    [Inject]
    private MicInputSaverModSettings modSettings;

    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene != EScene.SingScene)
        {
            return;
        }

        GameObject gameObject = new GameObject();
        gameObject.name = nameof(MicInputSaverMonoBehaviour);
        MicInputSaverMonoBehaviour behaviour = gameObject.AddComponent<MicInputSaverMonoBehaviour>();
        sceneEnteredContext.SceneInjector
            .WithBindingForInstance(modSettings)
            .Inject(behaviour);
    }
}

public class MicInputSaverMonoBehaviour : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    public static Dictionary<PlayerProfile, float[]> PlayerProfileToTargetArray { get; private set; } = new Dictionary<PlayerProfile, float[]>();
    public static Dictionary<PlayerProfile, int> PlayerProfileToTargetArrayIndex = new Dictionary<PlayerProfile, int>();

    [Inject]
    private MicInputSaverModSettings modSettings;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongMeta songMeta;

    private bool isInitialized;

    public void OnInjectionFinished()
    {
    }

    private void Update()
    {
        if (!isInitialized
            && songAudioPlayer.IsFullyLoaded)
        {
            isInitialized = true;
            Initialize();
        }
    }

    private void Initialize()
    {
        Debug.Log($"{nameof(MicInputSaverMonoBehaviour)} initializing");

        PlayerProfileToTargetArray.Clear();
        PlayerProfileToTargetArrayIndex.Clear();

        singSceneControl.PlayerControls.ForEach(playerControl =>
        {
            Debug.Log($"{nameof(MicInputSaverMonoBehaviour)} initializing {playerControl.PlayerProfile.Name}");
            if (playerControl.MicProfile == null)
            {
                return;
            }
            MicSampleRecorder micSampleRecorder = playerControl.PlayerMicPitchTracker.MicSampleRecorder;

            // Prepare TargetArray. Thereby, multiply sample count by 1.5 for additional buffer
            long maxMicSampleCount = (long)(songAudioPlayer.DurationInSeconds * micSampleRecorder.FinalSampleRate.Value * 1.5);
            PlayerProfileToTargetArray[playerControl.PlayerProfile] = new float[maxMicSampleCount];

            // Listen to mic samples of each player.
            // Add the samples to the corresponding target array.
            micSampleRecorder.RecordingEventStream.Subscribe(evt =>
            {
                // Check if the target array for this player is already initialized
                if (!PlayerProfileToTargetArray.TryGetValue(playerControl.PlayerProfile, out float[] micSamples))
                {
                    return;
                }

                // Copy samples to target array for this player
                PlayerProfileToTargetArrayIndex.TryGetValue(playerControl.PlayerProfile, out int micSamplesIndex);
                Array.Copy(micSampleRecorder.MicSamples, evt.NewSamplesStartIndex, micSamples, micSamplesIndex, evt.NewSampleCount);
                PlayerProfileToTargetArrayIndex[playerControl.PlayerProfile] = micSamplesIndex + evt.NewSampleCount;
            });
        });
    }

    private void OnDestroy()
    {
        SaveTargetArrays();
    }

    private void SaveTargetArrays()
    {
        string targetDirectory = GetTargetDirectory();
        Directory.CreateDirectory(targetDirectory);
        Debug.Log($"{nameof(MicInputSaverMonoBehaviour)} saving mic input to {targetDirectory}");

        PlayerProfileToTargetArray.ForEach(entry =>
        {
            PlayerProfile playerProfile = entry.Key;
            float[] micSamples = entry.Value;

            int writtenSamplesCount = PlayerProfileToTargetArrayIndex[playerProfile];
            float[] writtenMicSamples = new float[writtenSamplesCount];
            Array.Copy(micSamples, writtenMicSamples, writtenSamplesCount);

            PlayerControl playerControl = singSceneControl.PlayerControls
                .Where(it => it.PlayerProfile == playerProfile)
                .FirstOrDefault();
            if (playerControl == null)
            {
                return;
            }
            MicSampleRecorder micSampleRecorder = playerControl.PlayerMicPitchTracker.MicSampleRecorder;

            int channels = 1;
            int sampleRate = micSampleRecorder.FinalSampleRate.Value;
            string targetFilePath = $"{targetDirectory}/{DateTime.Now:yyyy-MM-dd-HH-mm-ss} - {SongMetaUtils.GetArtistDashTitle(songMeta)} - {playerProfile.Name}.wav";
            WavFileWriter.WriteFile(targetFilePath, sampleRate, channels, writtenMicSamples);

            string logMessage = $"Mic input of '{playerProfile.Name}' saved to '{targetFilePath}'";
            Debug.Log(logMessage);
            NotificationManager.CreateNotification(Translation.Of(logMessage));
        });
    }

    private string GetTargetDirectory()
    {
        return !modSettings.targetDirectory.IsNullOrEmpty()
            ? modSettings.targetDirectory
            : ApplicationUtils.GetPersistentDataPath("MicInputRecordings");
    }
}