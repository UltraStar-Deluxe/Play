using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(AudioSource))]
public class MicSampleRecorder : MonoBehaviour, INeedInjection
{
    private const int DefaultSampleRate = 44100;

    public bool playRecordedAudio;

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

    private readonly Subject<RecordingEvent> recordingEventStream = new();
    public IObservable<RecordingEvent> RecordingEventStream => recordingEventStream;

    [Inject(SearchMethod = SearchMethods.GetComponent)]
    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private int lastSamplePosition;

    private void OnDisable()
    {
        StopRecording();
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
            Debug.LogError("missing MicProfile");
            return;
        }
        if (MicProfile.IsInputFromConnectedClient)
        {
            Debug.LogWarning("Cannot record mic samples using connected client");
            return;
        }

        IsRecording.Value = true;

        // Check for microphone existence.
        string[] micDevices = Microphone.devices;
        if (!micDevices.Contains(micProfile.Name))
        {
            IsRecording.Value = false;
            Debug.LogWarning($"Did not find mic '{micProfile.Name}'. Available mic devices: {micDevices.ToCsv()}");
            return;
        }

        Debug.Log($"Starting recording with '{MicProfile.Name}' at {FinalSampleRate} Hz");

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        micAudioClip = Microphone.Start(MicProfile.Name, true, 1, FinalSampleRate.Value);
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();
        while (Microphone.GetPosition(MicProfile.Name) <= 0)
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
        audioSource.clip = micAudioClip;
        audioSource.loop = true;
    }

    public void StopRecording()
    {
        if (!IsRecording.Value)
        {
            return;
        }

        Debug.Log($"Stopping recording with '{MicProfile.Name}'");
        if (!MicProfile.IsInputFromConnectedClient)
        {
            Microphone.End(MicProfile.Name);
        }
        // Reset mic buffer
        for (int i = 0; i < MicSamples.Length; i++)
        {
            MicSamples[i] = 0;
        }
        
        IsRecording.Value = false;
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
        int currentSamplePosition = Microphone.GetPosition(MicProfile.Name);
        micAudioClip.GetData(MicSamples, currentSamplePosition);
        if (currentSamplePosition == lastSamplePosition)
        {
            // No new samples yet (or all samples changed, which is unlikely because the buffer has a length of 1 second and FPS should be > 1).
            return;
        }

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
        if (playRecordedAudio && !audioSource.isPlaying)
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
        Microphone.GetDeviceCaps(deviceName, out int minSampleRate, out int maxSampleRate);
        int finalSampleRate;
        if (targetSampleRate <= 0)
        {
            // Select best sample rate
            finalSampleRate = GetMaxSampleRate(maxSampleRate);
        }
        else
        {
            // Select the target sample rate.
            finalSampleRate = targetSampleRate;
        }

        return finalSampleRate;
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
