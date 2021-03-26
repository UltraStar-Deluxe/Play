using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicSampleRecorder : MonoBehaviour
{
    private const int DefaultSampleRateHz = 44100;

    public bool playRecordedAudio;

    private int micAmplifyMultiplier;
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

    // SampleRateHz is available after the MicProfile has been set.
    public int SampleRateHz { get; private set; }
    // The MicSamples array has the length of the SampleRateHz (one float value per sample.)
    public float[] MicSamples { get; private set; }

    private Subject<RecordingEvent> recordingEventStream = new Subject<RecordingEvent>();
    public IObservable<RecordingEvent> RecordingEventStream
    {
        get
        {
            return recordingEventStream;
        }
    }

    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private int lastSamplePosition;

    public bool IsRecording { get; private set; }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnDisable()
    {
        StopRecording();
    }

    void Update()
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

        IsRecording = true;

        // Check for microphone existence.
        if (CheckMicProfileIsDisconnected(micProfile))
        {
            IsRecording = false;
            return;
        }

        Debug.Log($"Starting recording with '{MicProfile.Name}' at {SampleRateHz} Hz");

        micAmplifyMultiplier = micProfile.AmplificationMultiplier;

        if (!micProfile.IsInputFromConnectedClient)
        {
            // Code for low-latency microphone input taken from
            // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
            micAudioClip = Microphone.Start(MicProfile.Name, true, 1, SampleRateHz);
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
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
        IsRecording = false;
    }

    private void UpdateRecording()
    {
        if (!IsRecording)
        {
            return;
        }

        if (MicProfile.IsInputFromConnectedClient)
        {
            UpdateRecordingWithInputFromConnectedClient();
        }
        else
        {
            UpdateRecordingWithInputFromMicrophoneOfThisDevice();
        }
    }

    private void UpdateRecordingWithInputFromMicrophoneOfThisDevice()
    {
        if (micAudioClip == null)
        {
            Debug.LogError("AudioClip for microphone is null");
            StopRecording();
            return;
        }
        
        // Fill buffer with raw sample data from microphone
        int currentSamplePosition = Microphone.GetPosition(MicProfile.Name);
        micAudioClip.GetData(MicSamples, currentSamplePosition);

        int newSamplesCount = GetNewSampleCountInCircularBuffer(lastSamplePosition, currentSamplePosition, MicSamples.Length);
        lastSamplePosition = currentSamplePosition;
        
        ApplyAmplificationToNewSamplesAndNotifyListeners(newSamplesCount);
    }

    private void UpdateRecordingWithInputFromConnectedClient()
    {
        // Use the sample data that is sent by the connected client
        if (!ClientConnectionManager.TryGetConnectedClientHandler(MicProfile.ConnectedClientId, out ConnectedClientHandler connectedClientHandler))
        {
            Debug.Log($"Client disconnected: {micProfile.Name}. Stopping recording.");
            StopRecording();
            return;
        }
        
        int newSamplesCount = connectedClientHandler.GetNewMicSamples(MicSamples);
        ApplyAmplificationToNewSamplesAndNotifyListeners(newSamplesCount);
    }

    private void ApplyAmplificationToNewSamplesAndNotifyListeners(int newSamplesCount)
    {
        // Process the portion that has been buffered by Unity since the last frame.
        // The buffer is filled "from the right", i.e., highest index holds the newest sample.
        int newSamplesStartIndex = MicSamples.Length - newSamplesCount;
        int newSamplesEndIndex = MicSamples.Length - 1;
        ApplyAmplification(MicSamples, newSamplesStartIndex, newSamplesEndIndex, micAmplifyMultiplier);

        // Notify listeners
        RecordingEvent recordingEvent = new RecordingEvent(MicSamples, newSamplesStartIndex, newSamplesEndIndex);
        recordingEventStream.OnNext(recordingEvent);
    }
    
    private static void ApplyAmplification(float[] buffer, int startIndex, int endIndex, float amplification)
    {
        if (amplification == 0)
        {
            return;
        }
        float newSample;
        for (int index = startIndex; index < endIndex; index++)
        {
            newSample = buffer[index] * amplification;
            if (newSample > 1)
            {
                newSample = 1;
            }
            else if (newSample < -1)
            {
                newSample = -1;
            }
            buffer[index] = newSample;
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
    
    private static int GetSampleRateHz(MicProfile micProfile)
    {
        if (micProfile.IsInputFromConnectedClient)
        {
            return ClientConnectionManager.TryGetConnectedClientHandler(micProfile.ConnectedClientId, out ConnectedClientHandler connectedClientHandler)
                ? connectedClientHandler.SampleRateHz
                : DefaultSampleRateHz;
        }
        else
        {
            Microphone.GetDeviceCaps(micProfile.Name, out int minFrequency, out int maxFrequency);
            return maxFrequency == 0
                // a value of zero indicates, that the device supports any frequency
                // https://docs.unity3d.com/ScriptReference/Microphone.GetDeviceCaps.html
                ? DefaultSampleRateHz
                : maxFrequency;
        }
    }
    
    private static bool CheckMicProfileIsDisconnected(MicProfile localMicProfile)
    {
        if (localMicProfile.IsInputFromConnectedClient)
        {
            if (!ClientConnectionManager.TryGetConnectedClientHandler(localMicProfile.ConnectedClientId, out ConnectedClientHandler connectedClientHandler))
            {
                Debug.LogWarning($"Client for mic-input disconnected: '{localMicProfile.ConnectedClientId}'. Stopping recording.");
                return true;
            }
        }
        else
        {
            List<string> devices = new List<string>(Microphone.devices);
            if (!devices.Contains(localMicProfile.Name))
            {
                Debug.LogWarning($"Did not find mic '{localMicProfile.Name}'. Available mic devices: {devices.ToCsv()}");
                return true;
            }
        }
        return false;
    }
}
