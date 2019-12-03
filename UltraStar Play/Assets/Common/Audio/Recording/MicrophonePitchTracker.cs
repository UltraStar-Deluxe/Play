using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophonePitchTracker : MonoBehaviour
{
    // TODO: use 44100Hz (if supported) or fallback to default microphone sample rate
    private const int SampleRateHz = 44100;

    public bool playRecordedAudio;

    [ReadOnly]
    public string lastMidiNoteName;

    private string micDevice;
    public string MicDevice
    {
        get
        {
            return micDevice;
        }
        set
        {
            bool restartPitchDetection = startedPitchDetection;
            if (startedPitchDetection)
            {
                StopPitchDetection();
            }
            micDevice = value;
            if (restartPitchDetection && !string.IsNullOrEmpty(micDevice))
            {
                StartPitchDetection();
            }
        }
    }

    public float[] MicData { get; private set; } = new float[SampleRateHz];
    public float[] PitchDetectionBuffer { get; private set; } = new float[SampleRateHz];

    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private bool startedPitchDetection;

    private readonly Subject<PitchEvent> pitchEventStream = new Subject<PitchEvent>();
    public IObservable<PitchEvent> PitchEventStream
    {
        get
        {
            return pitchEventStream;
        }
    }

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    void Awake()
    {
        audioSamplesAnalyzer = new CamdAudioSamplesAnalyzer(pitchEventStream, SampleRateHz);

        // Update label in inspector for debugging.
        pitchEventStream.Subscribe(pitchEvent => lastMidiNoteName = ((pitchEvent.MidiNote > 0)
                                                                    ? MidiUtils.GetAbsoluteName(pitchEvent.MidiNote)
                                                                    : ""));
    }

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        audioSamplesAnalyzer.Enable();
    }

    void OnDisable()
    {
        StopPitchDetection();
        audioSamplesAnalyzer.Disable();
    }

    void Update()
    {
        UpdateMicrophoneAudioPlayback();
        UpdatePitchDetection();
    }

    public void StartPitchDetection()
    {
        if (startedPitchDetection)
        {
            Debug.Log("Mic recoding already started.");
            return;
        }

        startedPitchDetection = true;
        List<string> soundcards = new List<string>(Microphone.devices);

        // Check for microphone existence.
        if (!soundcards.Contains(MicDevice))
        {
            string micDevicesCsv = string.Join(",", soundcards);
            Debug.LogError($"Did not find mic '{MicDevice}'. Available mic devices: {micDevicesCsv}");
            startedPitchDetection = false;
            return;
        }
        Debug.Log($"Start recording with '{MicDevice}'");

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        // It seems that there is still a latency of more than 200ms, which is too much for real-time processing.
        micAudioClip = Microphone.Start(MicDevice, true, 1, SampleRateHz);
        while (Microphone.GetPosition(MicDevice) <= 0) { /* Busy waiting */ }

        // Configure audio playback
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = micAudioClip;
        audioSource.loop = true;
    }

    public void StopPitchDetection()
    {
        if (!startedPitchDetection)
        {
            Debug.Log("Mic recording already stopped.");
            return;
        }

        Debug.Log($"Stop recording with '{MicDevice}'");
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
        int currentSamplePosition = Microphone.GetPosition(MicDevice);
        micAudioClip.GetData(MicData, currentSamplePosition);

        // Prepare the portion that should be analyzed by the pitch detection library.
        // In every frame, the mic buffer (which has a length of 1 second)
        // that was generated since the last frame has to be analyzed.
        int samplesSinceLastFrame = (int)(SampleRateHz * Time.deltaTime);

        // The new samples are coming in from the "right side" by Unity, i.e. the newest sample is at MicData.Length-1
        // The pitch detection lib analyzes its buffer from 0 to a given length (without the option for an offset).
        // Thus, we have to move the new samples in the mic buffer to the beginning of the buffer-to-be-analyzed.
        Array.Copy(MicData, SampleRateHz - samplesSinceLastFrame, PitchDetectionBuffer, 0, samplesSinceLastFrame);

        // Clear the PitchDetection buffer that is not analyzed in this frame (this is not really needed).
        for (int i = samplesSinceLastFrame; i < SampleRateHz; i++)
        {
            PitchDetectionBuffer[i] = 0;
        }

        // Detect the pitch of the sample.
        audioSamplesAnalyzer.ProcessAudioSamples(PitchDetectionBuffer, samplesSinceLastFrame);
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