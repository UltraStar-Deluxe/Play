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

    [Inject]
    private ModObjectContext modObjectContext;

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
            .WithBindingForInstance(modObjectContext)
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
    private ModObjectContext modObjectContext;

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
        PlayerProfileToTargetArray.ForEach(entry =>
        {
            PlayerProfile playerProfile = entry.Key;
            float[] micSamples = entry.Value;
            SaveTargetArray(playerProfile, micSamples);
        });

        NotificationManager.CreateNotification(Translation.Of($"Mic input saved to {GetTargetDirectory()}"));
    }

    private void SaveTargetArray(PlayerProfile playerProfile, float[] micSamples)
    {
        int micSampleRate = GetMicSampleRate(playerProfile);
        if (micSampleRate <= 0)
        {
            return;
        }

        // Find written samples
        int writtenSamplesCount = PlayerProfileToTargetArrayIndex[playerProfile];
        float[] writtenMicSamples = new float[writtenSamplesCount];
        Array.Copy(micSamples, writtenMicSamples, writtenSamplesCount);

        // Shift samples by mic delay
        int micDelayInMillis = GetMicDelayInMillis(playerProfile);
        int micDelayInSamples = (int)(micDelayInMillis * 0.001 * micSampleRate);
        AudioMixerUtils.Shift(writtenMicSamples, -micDelayInSamples);

        // Normalize samples
        AudioMixerUtils.Normalize(writtenMicSamples, 0.75f);

        // Save samples
        int channels = 1;
        string targetFilePath = GetTargetFilePath(playerProfile);
        WavFileWriter.WriteFile(targetFilePath, micSampleRate, channels, writtenMicSamples);
        Debug.Log($"Mic input of '{playerProfile.Name}' saved to '{targetFilePath}'");

        // Save mix with instrumental audio
        string instrumentalAudioFilePath = SongMetaUtils.GetInstrumentalAudioUri(songMeta);
        if(File.Exists(instrumentalAudioFilePath))
        {
            AudioClip audioClip = AudioManager.LoadAudioClipFromUriImmediately(instrumentalAudioFilePath, false);
            int instrumentalSampleRate = audioClip.frequency;
            float[] resampledMicSamples = AudioMixerUtils.Resample(writtenMicSamples, micSampleRate, instrumentalSampleRate);
            float[] instrumentalSamples = GetMonoSamples(audioClip);
            float[] mixedSamples = AudioMixerUtils.Mix(instrumentalSamples, resampledMicSamples);

            string mixTargetFilePath = GetTargetFilePath(playerProfile, " - mixed");
            WavFileWriter.WriteFile(mixTargetFilePath, instrumentalSampleRate, channels, mixedSamples);
            Debug.Log($"Mix of instrumental and mic input of '{playerProfile.Name}' saved to '{mixTargetFilePath}'");
        }
    }

    private float[] GetMonoSamples(AudioClip audioClip)
    {
        // Get the total number of samples and channels
        int totalSamples = audioClip.samples;
        int channels = audioClip.channels;

        // Retrieve the audio data
        float[] multiChannelSamples = new float[totalSamples * channels];
        audioClip.GetData(multiChannelSamples, 0);

        // Convert to mono
        float[] monoSamples = AudioUtils.ToMonoAudioSamples(multiChannelSamples, channels);
        return monoSamples;
    }

    private int GetMicSampleRate(PlayerProfile playerProfile)
    {
        PlayerControl playerControl = GetPlayerControl(playerProfile);
        if (playerControl == null)
        {
            return 0;
        }
        return playerControl.PlayerMicPitchTracker.MicSampleRecorder.FinalSampleRate.Value;
    }

    private int GetMicDelayInMillis(PlayerProfile playerProfile)
    {
        PlayerControl playerControl = GetPlayerControl(playerProfile);
        if (playerControl == null)
        {
            return 0;
        }
        return playerControl.PlayerMicPitchTracker.MicSampleRecorder.MicProfile.DelayInMillis;
    }

    private string GetTargetDirectory()
    {
        return !modSettings.targetDirectory.IsNullOrEmpty()
            ? modSettings.targetDirectory
            : $"{modObjectContext.ModPersistentDataFolder}/Recordings";
    }

    private string GetTargetFilePath(PlayerProfile playerProfile, string suffix="")
    {
        string targetDirectory = GetTargetDirectory();
        Directory.CreateDirectory(targetDirectory);
        Debug.Log($"{nameof(MicInputSaverMonoBehaviour)} saving mic input to {targetDirectory}");

        return $"{targetDirectory}/{SongMetaUtils.GetArtistDashTitle(songMeta)} - {playerProfile.Name}{suffix}.wav";
    }

    private PlayerControl GetPlayerControl(PlayerProfile playerProfile)
    {
        return singSceneControl.PlayerControls.FirstOrDefault(it => it.PlayerProfile == playerProfile);
    }
}