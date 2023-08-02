using System;
using System.Linq;
using PortAudioForUnity;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicSampleRecorder : MonoBehaviour
{
    public const int DefaultSampleRate = 44100;

    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get
        {
            return micProfile;
        }
        set
        {
            bool restartPitchDetection = IsRecording.Value;
            if (IsRecording.Value)
            {
                StopRecording();
            }
            micProfile = value;
            if (micProfile != null
                && !micProfile.IsInputFromConnectedClient
                && !micProfile.Name.IsNullOrEmpty())
            {
                FinalSampleRate.Value = GetFinalSampleRate(micProfile.Name, micProfile.SampleRate);
                MicSamples = new float[FinalSampleRate.Value];
                if (restartPitchDetection)
                {
                    StartRecording();
                }
            }
        }
    }

    public ReactiveProperty<bool> IsRecording { get; private set; } = new(false);
    
    // The sample rate is available after a MicProfile has been set.
    public ReactiveProperty<int> FinalSampleRate { get; private set; } = new(0);
    // The MicSamples array has one float value per sample.
    public float[] MicSamples { get; private set; } = new float[DefaultSampleRate];

    private bool playRecordedAudio;
    public bool PlayRecordedAudio
    {
        get
        {
            return playRecordedAudio;
        }
        set
        {
            bool wasRecording = IsRecording.Value;
            playRecordedAudio = value;
            if (wasRecording)
            {
                StopRecording();
                StartRecording();
            }
        }
    }

    public float Volume
    {
        get => audioSource.volume;
        set => audioSource.volume = value;
    }

    private readonly CountSubject<RecordingEvent> recordingEventStream = new();
    public IObservable<RecordingEvent> RecordingEventStream => recordingEventStream;

    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private int lastSamplePosition;

    private bool continueRecordingOnAddListener;
    private bool continueRecordingOnEnable;

    public string PortAudioOutputDeviceName { get; set; }

    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();

        recordingEventStream.Count.Subscribe(subscriberCount =>
        {
            if (subscriberCount <= 0
                && IsRecording.Value)
            {
                continueRecordingOnAddListener = true;
                Debug.Log($"Stopping recording because no subscribers left: {MicProfile.GetDisplayNameWithChannel()}");
                StopRecording();
            }
            else if (subscriberCount > 0
                     && !IsRecording.Value
                     && continueRecordingOnAddListener)
            {
                continueRecordingOnAddListener = false;
                Debug.Log($"Continue recording for new subscriber: {MicProfile.GetDisplayNameWithChannel()}");
                StartRecording();
            }
        });
    }
    
    private void OnEnable()
    {
        if (MicProfile != null
            && continueRecordingOnEnable)
        {
            Debug.Log($"Continue recording on enable: {MicProfile.GetDisplayNameWithChannel()}");
            StartRecording();
        }
    }
    
    private void OnDisable()
    {
        if (MicProfile != null
            && IsRecording.Value)
        {
            Debug.Log($"Stopping recording on disable: {MicProfile.GetDisplayNameWithChannel()}");
            continueRecordingOnEnable = true;
            StopRecording();
        }
    }

    private void Update()
    {
        UpdateMicrophoneAudioPlayback();
        UpdateRecording();
    }

    public void StartRecording()
    {
        if (IsRecording.Value)
        {
            return;
        }
        if (MicProfile == null)
        {
            Debug.LogError("MicSampleRecorder - Failed to start recording, missing MicProfile");
            return;
        }
        if (MicProfile.IsInputFromConnectedClient)
        {
            Debug.LogWarning("Cannot record mic samples using connected client");
            return;
        }
        
        IsRecording.Value = true;

        // Check for microphone existence.
        string[] micDevices = MicrophoneAdapter.Devices;
        if (!micDevices.Contains(micProfile.Name))
        {
            IsRecording.Value = false;
            Debug.LogWarning($"Did not find mic '{micProfile.Name}'. Available mic devices: {micDevices.ToCsv()}");
            return;
        }

        Debug.Log($"Starting recording with '{MicProfile.GetDisplayNameWithChannel()}' at {FinalSampleRate} Hz");

        string outputDeviceName = playRecordedAudio && MicrophoneAdapter.UsePortAudio
            ? GetFinalPortAudioOutputDeviceName()
            : "";

        // Code for low-latency Unity microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        DestroyAudioClips();
        using DisposableStopwatch d = new($"MicrophoneAdapter.Start took <ms> with {MicProfile.GetDisplayNameWithChannel()}");
        {
            micAudioClip = MicrophoneAdapter.Start(MicProfile.Name, true, 1, FinalSampleRate.Value, outputDeviceName);
        }
        
        if (!MicrophoneAdapter.UsePortAudio)
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();
            while (MicrophoneAdapter.GetPosition(MicProfile.Name) <= 0)
            {
                // <Busy waiting>
                // Emergency exit
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    IsRecording.Value = false;
                    Debug.LogError("Microphone did not provide any samples. Took emergency exit out of busy waiting.");
                    return;
                }
            }
        }

        // Configure audio playback
        if (micAudioClip != null)
        {
            audioSource.clip = micAudioClip;
            audioSource.loop = true;
        }
    }

    private string GetFinalPortAudioOutputDeviceName()
    {
        if (PortAudioOutputDeviceName.IsNullOrEmpty())
        {
            return PortAudioUtils.DefaultOutputDeviceInfo.Name;
        }

        return PortAudioOutputDeviceName;
    }

    public void StopRecording()
    {
        if (!IsRecording.Value)
        {
            return;
        }

        IsRecording.Value = false;

        Debug.Log($"Stopping recording with '{MicProfile.GetDisplayNameWithChannel()}'");
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
        DestroyAudioClips();

        if (!MicProfile.IsInputFromConnectedClient
            && MicrophoneAdapter.Devices.Contains(MicProfile.Name))
        {
            MicrophoneAdapter.End(MicProfile.Name);
        }
        // Reset mic buffer
        for (int i = 0; i < MicSamples.Length; i++)
        {
            MicSamples[i] = 0;
        }
    }

    private void UpdateRecording()
    {
        if (!IsRecording.Value)
        {
            return;
        }

        if (micAudioClip == null && !MicrophoneAdapter.UsePortAudio)
        {
            Debug.LogError("AudioClip from Unity microphone recording is null");
            StopRecording();
            return;
        }

        // Fill buffer with raw sample data from microphone
        int currentSamplePosition = MicrophoneAdapter.GetPosition(MicProfile.Name);
        if (currentSamplePosition == lastSamplePosition)
        {
            // No new samples yet (or all samples changed, which is unlikely because the buffer has a length of 1 second and FPS should be > 1).
            return;
        }
        MicrophoneAdapter.GetRecordedSamples(MicProfile.Name, MicProfile.ChannelIndex, micAudioClip, currentSamplePosition, MicSamples);

        int newSamplesCount = GetNewSampleCountInCircularBuffer(lastSamplePosition, currentSamplePosition, MicSamples.Length);
        NotifyListeners(newSamplesCount);

        lastSamplePosition = currentSamplePosition;
    }

    private void NotifyListeners(int newSamplesCount)
    {
        // Notify listeners
        if (newSamplesCount <= 0)
        {
            return;
        }
        int newSamplesStartIndex = MicSamples.Length - newSamplesCount;
        int newSamplesEndIndex = MicSamples.Length - 1;
        RecordingEvent recordingEvent = new(MicSamples, newSamplesStartIndex, newSamplesEndIndex);
        recordingEventStream.OnNext(recordingEvent);
    }

    private void UpdateMicrophoneAudioPlayback()
    {
        if (MicrophoneAdapter.UsePortAudio)
        {
            return;
        }

        if (playRecordedAudio && !audioSource.isPlaying && audioSource.clip != null)
        {
            audioSource.Play();
        }
        else if (!playRecordedAudio && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    private static int GetNewSampleCountInCircularBuffer(int lastSamplePosition, int currentSamplePosition, int bufferLength)
    {
        // Check if the recording re-started from index 0 after reaching the end of the buffer.
        if (currentSamplePosition <= lastSamplePosition)
        {
            return (bufferLength - lastSamplePosition) + currentSamplePosition;
        }
        else
        {
            return currentSamplePosition - lastSamplePosition;
        }
    }

    public static int GetFinalSampleRate(string deviceName, int targetSampleRate)
    {
        if (targetSampleRate > 0)
        {
            // Use explicitly set sample rate
            return targetSampleRate;
        }

        // Use best available sample rate
        if (!MicrophoneAdapter.Devices.Contains(deviceName))
        {
            return DefaultSampleRate;
        }
        MicrophoneAdapter.GetDeviceCaps(deviceName, out int minSampleRate, out int maxSampleRate, out int channelCount);
        return GetMaxSampleRate(maxSampleRate);
    }

    private void OnDestroy()
    {
        DestroyAudioClips();
    }

    private void DestroyAudioClips()
    {
        if (micAudioClip != null)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
            Destroy(micAudioClip);
            micAudioClip = null;
        }
    }

    private static int GetMaxSampleRate(int maxSampleRate)
    {
        // Select best available sample rate.
        if (maxSampleRate == 0)
        {
            // A max value of 0 indicates that any sample rate can be used
            return DefaultSampleRate;
        }
        else if (maxSampleRate == 16000)
        {
            // Unity returns a value of 16000 on some devices, although more is possible.
            // Every half-decent smartphone should be able to record with a better sample rate than this.
            // See https://issuetracker.unity3d.com/issues/mobile-incorrect-values-returned-from-microphone-dot-getdevicecaps
            return DefaultSampleRate;
        }
        else
        {
            return maxSampleRate;
        }
    }
}
