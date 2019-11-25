using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
abstract public class AbstractMicPitchTracker : MonoBehaviour
{
    public const int SampleRate = 22050;

    public bool playRecordedAudio;

    private string micDevice;
    public string MicDevice
    {
        get
        {
            return micDevice;
        }
        set
        {
            bool restartPitchDetection = startedMicRecording;
            if (startedMicRecording)
            {
                StopMicRecording();
            }
            micDevice = value;
            if (restartPitchDetection && !string.IsNullOrEmpty(micDevice))
            {
                StartMicRecording();
            }
        }
    }

    public float[] MicData { get; private set; } = new float[SampleRate];
    public float[] PitchDetectionBuffer { get; private set; } = new float[SampleRate];

    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private bool startedMicRecording;

    protected readonly Subject<PitchEvent> pitchEventStream = new Subject<PitchEvent>();
    public IObservable<PitchEvent> PitchEventStream
    {
        get
        {
            return pitchEventStream;
        }
    }

    abstract protected void EnablePitchTracker();
    abstract protected void DisablePitchTracker();
    abstract protected void ProcessMicData(float[] pitchDetectionBuffer, int samplesSinceLastFrame);

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        EnablePitchTracker();
    }

    void OnDisable()
    {
        StopMicRecording();
        DisablePitchTracker();
    }

    void Update()
    {
        UpdateMicrophoneAudioPlayback();
        UpdatePitchDetection();
    }

    public void StartMicRecording()
    {
        if (startedMicRecording)
        {
            Debug.Log("Mic recoding already started.");
            return;
        }

        startedMicRecording = true;
        List<string> soundcards = new List<string>(Microphone.devices);

        // Check for microphone existence.
        if (!soundcards.Contains(MicDevice))
        {
            string micDevicesCsv = string.Join(",", soundcards);
            Debug.LogError($"Did not find mic '{MicDevice}'. Available mic devices: {micDevicesCsv}");
            startedMicRecording = false;
            return;
        }
        Debug.Log($"Start recording with '{MicDevice}'");

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        // It seems that there is still a latency of more than 200ms, which is too much for real-time processing.
        micAudioClip = Microphone.Start(MicDevice, true, 1, SampleRate);
        while (Microphone.GetPosition(MicDevice) <= 0) { /* Busy waiting */ }

        // Configure audio playback
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = micAudioClip;
        audioSource.loop = true;
    }

    public void StopMicRecording()
    {
        if (!startedMicRecording)
        {
            Debug.Log("Mic recording already stopped.");
            return;
        }

        Debug.Log($"Stop recording with '{MicDevice}'");
        Microphone.End(MicDevice);
        startedMicRecording = false;
    }

    private void UpdatePitchDetection()
    {
        if (!startedMicRecording)
        {
            return;
        }

        if (micAudioClip == null)
        {
            Debug.LogError("AudioClip for microphone is null");
            return;
        }

        // Fill buffer with raw sample data from microphone
        int currentSamplePosition = Microphone.GetPosition(MicDevice);
        micAudioClip.GetData(MicData, currentSamplePosition);

        // Prepare the portion that should be analyzed by the pitch detection library.
        // In every frame, the mic buffer (which has a length of 1 second)
        // that was generated since the last frame has to be analyzed.
        int samplesSinceLastFrame = (int)(SampleRate * Time.deltaTime);

        // The new samples are coming in from the "right side" by Unity, i.e. the newest sample is at MicData.Length-1
        // The pitch detection lib analyzes its buffer from 0 to a given length (without the option for an offset).
        // Thus, we have to move the new samples in the mic buffer to the beginning of the buffer-to-be-analyzed.
        Array.Copy(MicData, SampleRate - samplesSinceLastFrame, PitchDetectionBuffer, 0, samplesSinceLastFrame);

        // Clear the PitchDetection buffer that is not analyzed in this frame (this is not really needed).
        for (int i = samplesSinceLastFrame; i < SampleRate; i++)
        {
            PitchDetectionBuffer[i] = 0;
        }

        // Detect the pitch of the sample.
        ProcessMicData(PitchDetectionBuffer, samplesSinceLastFrame);
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
