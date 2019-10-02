using System;
using System.Collections;
using System.Collections.Generic;
using Pitch;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophonePitchTracker : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const int BufferSteps = 10;
    private const int BufferSize = SampleRate / BufferSteps;

    public bool playRecordedAudio;

    public string MicDevice { get; set; }

    private AudioSource audioSource;
    private float[] audioClipData = new float[BufferSize];

    private PitchTracker pitchTracker = new PitchTracker();
    private bool startedPitchDetection;

    public void AddPitchDetectedHandler(PitchTracker.PitchDetectedHandler handler)
    {
        pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(handler);
    }

    public void RemovePitchDetectedHandler(PitchTracker.PitchDetectedHandler handler)
    {
        pitchTracker.PitchDetected -= new PitchTracker.PitchDetectedHandler(handler);
    }

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        // Initialize the pitch tracker
        pitchTracker.PitchRecordHistorySize = 5;
        pitchTracker.SampleRate = SampleRate;
    }

    void OnDisable()
    {
        StopPitchDetection();
    }

    void Update()
    {
        // Enable / disable microphone audio playback
        if (playRecordedAudio && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!playRecordedAudio && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        UpdatePitchDetection();
    }

    public void StartPitchDetection()
    {
        startedPitchDetection = true;
        // Check for microphone existence.
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError($"No mic devices found.");
            gameObject.SetActive(false);
            return;
        }
        List<string> micDeviceNames = new List<string>(Microphone.devices);
        if (!micDeviceNames.Contains(MicDevice))
        {
            string micDevicesCsv = String.Join(",", micDeviceNames);
            Debug.LogError($"Did not find mic '{MicDevice}'. Available mic devices: {micDevicesCsv}");
            gameObject.SetActive(false);
            return;
        }
        Debug.Log($"Start recording with '{MicDevice}'");

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        // It seems that there is still a latency of more than 200ms, which is too much for real-time processing.
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(MicDevice, true, 1, SampleRate);
        audioSource.loop = true;
        if (playRecordedAudio)
        {
            while (!(Microphone.GetPosition(null) > 0)) { /* Busy waiting */ }
            // Debug.Log("Start playing... position is " + Microphone.GetPosition(null));
            audioSource.Play();
        }
    }

    public void StopPitchDetection()
    {
        Microphone.End(MicDevice);
        startedPitchDetection = false;
    }

    private void UpdatePitchDetection()
    {
        if (!startedPitchDetection)
        {
            return;
        }

        // Fill buffer with raw sample data from microphone
        if (audioSource.clip == null)
        {
            Debug.LogWarning("AudioSource.clip is null");
            return;
        }
        audioSource.clip.GetData(audioClipData, BufferSize);

        // Detect the pitch of the sample
        pitchTracker.ProcessBuffer(audioClipData, 0);
    }
}
