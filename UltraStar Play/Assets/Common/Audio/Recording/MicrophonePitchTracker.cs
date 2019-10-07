using System;
using System.Collections;
using System.Collections.Generic;
using Pitch;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophonePitchTracker : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const int BufferSize = SampleRate / 5;

    public bool playRecordedAudio;

    public string MicDevice { get; set; }
    public float[] MicData { get; private set; } = new float[SampleRate];
    public float[] MicDataBuffer { get; private set; } = new float[BufferSize];

    private AudioSource audioSource;
    private AudioClip micAudioClip;

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
        UpdateMicrophoneAudioPlayback();

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
        micAudioClip = Microphone.Start(MicDevice, true, 1, SampleRate);
        while (!(Microphone.GetPosition(null) > 0)) { /* Busy waiting */ }

        // Configure audio playback
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = micAudioClip;
        audioSource.loop = true;
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

        if (micAudioClip == null)
        {
            Debug.LogError("AudioClip for microphone is null");
            return;
        }

        // Fill buffer with raw sample data from microphone
        int currentSample = Microphone.GetPosition(null);
        micAudioClip.GetData(MicData, currentSample);
        // For analysis, only use a portion of the complete microphone data.
        Array.Copy(MicData, SampleRate - BufferSize, MicDataBuffer, 0, BufferSize);

        // Detect the pitch of the sample.
        pitchTracker.ProcessBuffer(MicDataBuffer, 0);
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
}
