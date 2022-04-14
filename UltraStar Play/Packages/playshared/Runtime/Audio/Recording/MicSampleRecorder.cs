using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(AudioSource))]
public class MicSampleRecorder : MonoBehaviour, INeedInjection
{
    private const int DefaultSampleRateHz = 44100;

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
            bool restartPitchDetection = IsRecording;
            if (IsRecording)
            {
                StopRecording();
            }
            micProfile = value;
            if (micProfile != null
                && ((!micProfile.IsInputFromConnectedClient && !micProfile.Name.IsNullOrEmpty())
                     || (micProfile.IsInputFromConnectedClient && !micProfile.ConnectedClientId.IsNullOrEmpty())))
            {
                SampleRateHz = GetSampleRateHz(micProfile);
                MicSamples = new float[SampleRateHz];
                if (restartPitchDetection)
                {
                    StartRecording();
                }
            }
        }
    }
    
    public bool IsRecording { get; private set; }
    
    // SampleRateHz is available after the MicProfile has been set.
    public int SampleRateHz { get; private set; }
    // The MicSamples array has the length of the SampleRateHz (one float value per sample.)
    public float[] MicSamples { get; private set; }

    private readonly Subject<RecordingEvent> recordingEventStream = new();
    public IObservable<RecordingEvent> RecordingEventStream => recordingEventStream;

    [Inject]
    public IServerSideConnectRequestManager serverSideConnectRequestManager;

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
        if (IsRecording)
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

        IsRecording = true;

        // Check for microphone existence.
        if (CheckMicProfileIsDisconnected(micProfile))
        {
            IsRecording = false;
            return;
        }

        Debug.Log($"Starting recording with '{MicProfile.Name}' at {SampleRateHz} Hz");

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        micAudioClip = Microphone.Start(MicProfile.Name, true, 1, SampleRateHz);
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();
        while (Microphone.GetPosition(MicProfile.Name) <= 0)
        {
            // <Busy waiting>
            // Emergency exit
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                IsRecording = false;
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
        if (!IsRecording)
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
        
        IsRecording = false;
    }

    private void UpdateRecording()
    {
        if (!IsRecording)
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
        ApplyAmplificationAndNotifyListeners(newSamplesCount);
        
        lastSamplePosition = currentSamplePosition;
    }

    private void ApplyAmplificationAndNotifyListeners(int newSamplesCount)
    {
        // The buffer is always overwritten completely by Unity. Thus, amplification has to be applied to the whole buffer again.
        // The buffer is filled "from the right", i.e., highest index holds the newest sample.
        if (micProfile.Amplification > 0)
        {
            ApplyAmplification(MicSamples, 0, MicSamples.Length - 1, micProfile.AmplificationMultiplier);
        }
        
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
    
    private static void ApplyAmplification(float[] buffer, int startIndex, int endIndex, float amplificationMultiplier)
    {
        for (int index = startIndex; index < endIndex; index++)
        {
            buffer[index] *= amplificationMultiplier;
        }
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
    
    private int GetSampleRateHz(MicProfile localMicProfile)
    {
        if (localMicProfile.IsInputFromConnectedClient)
        {
            return serverSideConnectRequestManager.TryGetConnectedClientHandler(localMicProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler)
                ? connectedClientHandler.SampleRateHz
                : DefaultSampleRateHz;
        }
        else
        {
            Microphone.GetDeviceCaps(localMicProfile.Name, out int minFrequency, out int maxFrequency);
            return maxFrequency == 0
                // a value of zero indicates, that the device supports any frequency
                // https://docs.unity3d.com/ScriptReference/Microphone.GetDeviceCaps.html
                ? DefaultSampleRateHz
                : maxFrequency;
        }
    }
    
    private bool CheckMicProfileIsDisconnected(MicProfile localMicProfile)
    {
        if (localMicProfile.IsInputFromConnectedClient)
        {
            if (!serverSideConnectRequestManager.TryGetConnectedClientHandler(localMicProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler))
            {
                Debug.LogWarning($"Client for mic-input not connected: '{localMicProfile.ConnectedClientId}'.");
                return true;
            }
        }
        else
        {
            List<string> devices = new(Microphone.devices);
            if (!devices.Contains(localMicProfile.Name))
            {
                Debug.LogWarning($"Did not find mic '{localMicProfile.Name}'. Available mic devices: {devices.ToCsv()}");
                return true;
            }
        }
        return false;
    }
}
