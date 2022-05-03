using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ClientSideMicSampleRecorder: MonoBehaviour, INeedInjection
{
    public static ClientSideMicSampleRecorder Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ClientSideMicSampleRecorder>("ClientSideMicSampleRecorder");
        }
    }

    private const int DefaultSampleRateHz = 48000;

    public ReactiveProperty<bool> IsRecording { get; private set; } = new ReactiveProperty<bool>();
    
    // The MicSamples array has the length of the SampleRateHz (one float value per sample.)
    public float[] MicSampleBuffer { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponent)]
    private AudioSource audioSource;
    
    [Inject]
    private Settings settings;
    
    private AudioClip micAudioClip;

    private int lastSamplePosition;

    private readonly Subject<RecordingEvent> recordingEventStream = new Subject<RecordingEvent>();
    public IObservable<RecordingEvent> RecordingEventStream => recordingEventStream;

    private void Start()
    {
        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(newValue =>
            {
                if (IsRecording.Value)
                {
                    // Restart recording with changed settings
                    StopRecording();
                    StartRecording();
                }
            });
    }

    private void Update()
    {
        if (IsRecording.Value)
        {
            UpdateRecording();
        }
    }

    public void StartRecording()
    {
        if (IsRecording.Value)
        {
            throw new UnityException("Already recording");
        }

        string deviceName = settings.MicProfile.Name;
        int sampleRate = GetFinalSampleRate(deviceName, settings.MicProfile.SampleRate);
        Debug.Log($"Starting recording with '{deviceName}' at {sampleRate} Hz (targetSampleRate: {settings.MicProfile.SampleRate})");

        if (MicSampleBuffer == null
            || MicSampleBuffer.Length != sampleRate)
        {
            MicSampleBuffer = new float[sampleRate];
        }

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        micAudioClip = Microphone.Start(deviceName, true, 1, sampleRate);
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        while (Microphone.GetPosition(deviceName) <= 0)
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

        // Configure audio playback
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = micAudioClip;
        audioSource.loop = true;
        
        IsRecording.Value = true;
    }
    
    private void UpdateRecording()
    {
        if (!IsRecording.Value)
        {
            return;
        }

        if (micAudioClip == null)
        {
            Debug.LogError("AudioClip for microphone is null");
            StopRecording();
            return;
        }

        // Fill buffer with raw sample data from microphone
        int currentSamplePosition = Microphone.GetPosition(settings.MicProfile.Name);
        micAudioClip.GetData(MicSampleBuffer, currentSamplePosition);
        if (currentSamplePosition == lastSamplePosition)
        {
            // No new samples yet (or all samples changed, which is unlikely because the buffer has a length of 1 second and FPS should be > 1).
            return;
        }

        // Process the portion that has been buffered by Unity since the last frame.
        // New samples come into the buffer "from the right", i.e., highest index holds the newest sample.
        int newSamplesCount = GetNewSampleCountInCircularBuffer(lastSamplePosition, currentSamplePosition);
        int newSamplesStartIndex = MicSampleBuffer.Length - newSamplesCount;
        int newSamplesEndIndex = MicSampleBuffer.Length - 1;

        // Notify listeners
        RecordingEvent recordingEvent = new RecordingEvent(MicSampleBuffer, newSamplesStartIndex, newSamplesEndIndex);
        recordingEventStream.OnNext(recordingEvent);

        lastSamplePosition = currentSamplePosition;
    }
    
    private int GetNewSampleCountInCircularBuffer(int lastSamplePosition, int currentSamplePosition)
    {
        int bufferLength = MicSampleBuffer.Length;

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
    
    public void StopRecording()
    {
        if (!IsRecording.Value)
        {
            return;
        }
        
        Debug.Log($"Stopping recording with '{settings.MicProfile.Name}'");
        IsRecording.Value = false;
        Microphone.End(settings.MicProfile.Name);
    }

    public static int GetFinalSampleRate(string deviceName, int targetSampleRate)
    {
        Microphone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
        int finalSampleRate = targetSampleRate;
        if (finalSampleRate == 0)
        {
            // Select best matching sample rate
            finalSampleRate = maxFreq;
        }
        if (finalSampleRate == 0)
        {
            // A value of 0 indicates that any sample rate can be used
            finalSampleRate = DefaultSampleRateHz;
        }

        if (finalSampleRate == 16000
            && targetSampleRate == 0)
        {
            // Unity returns a value of 16000 on some devices, although more is possible.
            // Every half-decent smartphone should be able to record with a better sample rate than this.
            // See https://issuetracker.unity3d.com/issues/mobile-incorrect-values-returned-from-microphone-dot-getdevicecaps
            finalSampleRate = DefaultSampleRateHz;
        }

        return finalSampleRate;
    }
}
