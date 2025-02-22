using UniInject;
using UniRx;
using UnityEngine;

public class MicRecorderMonoBehaviour : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private MicRecordingSaverModSettings modSettings;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Settings settings;

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
        Debug.Log($"{nameof(MicRecorderMonoBehaviour)} initializing");
        MicRecordingData.PlayerProfileToMicRecording.Clear();
        singSceneControl.PlayerControls.ForEach(InitializeMicRecording);
    }

    private void InitializeMicRecording(PlayerControl playerControl)
    {
        Debug.Log($"{nameof(MicRecorderMonoBehaviour)} initializing {playerControl.PlayerProfile.Name}");
        if (playerControl.MicProfile == null)
        {
            return;
        }
        MicSampleRecorder micSampleRecorder = playerControl.PlayerMicPitchTracker.MicSampleRecorder;

        // Prepare data for mic input recording. Thereby, multiply size of target array by 1.5 for additional buffer.
        long maxMicSampleCount = (long)(songAudioPlayer.DurationInSeconds * micSampleRecorder.FinalSampleRate.Value * 1.5);
        float[] micSamples = new float[maxMicSampleCount];

        int micSampleRate = micSampleRecorder.FinalSampleRate.Value;
        int overallDelayInMillis = micSampleRecorder.MicProfile.DelayInMillis + settings.SystemAudioBackendDelayInMillis + modSettings.audioShiftInMillis;

        MicRecordingData.PlayerProfileToMicRecording[playerControl.PlayerProfile] = new MicRecordingData(
            playerControl.PlayerProfile,
            micSamples,
            micSampleRate,
            overallDelayInMillis
        );

        // Listen to mic samples of each player and add the samples to the corresponding target array.
        micSampleRecorder.RecordingEventStream.Subscribe(evt =>
        {
            if (!MicRecordingData.PlayerProfileToMicRecording.TryGetValue(playerControl.PlayerProfile, out MicRecordingData micRecordingData))
            {
                return;
            }
            micRecordingData.AddSamples(evt);
        }).AddTo(gameObject);
    }
}