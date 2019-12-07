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
    [ReadOnly]
    public int lastMidiNote;

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
            bool restartPitchDetection = startedPitchDetection;
            if (startedPitchDetection)
            {
                StopPitchDetection();
            }
            micProfile = value;
            if (restartPitchDetection && micProfile != null && !string.IsNullOrEmpty(micProfile.Name))
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
        // Update label in inspector for debugging.
        pitchEventStream.Subscribe(UpdateLastMidiNoteFields);
    }

    private void UpdateLastMidiNoteFields(PitchEvent pitchEvent)
    {
        if (pitchEvent == null)
        {
            lastMidiNoteName = "";
            lastMidiNote = 0;
        }
        else
        {
            lastMidiNoteName = MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
            lastMidiNote = pitchEvent.MidiNote;
        }
    }

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        audioSamplesAnalyzer = new CamdAudioSamplesAnalyzer(SampleRateHz);
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
        List<string> soundcards = new List<string>(UnityEngine.Microphone.devices);

        // Check for microphone existence.
        if (!soundcards.Contains(MicProfile.Name))
        {
            string micDevicesCsv = string.Join(",", soundcards);
            Debug.LogError($"Did not find mic '{MicProfile.Name}'. Available mic devices: {micDevicesCsv}");
            startedPitchDetection = false;
            return;
        }
        Debug.Log($"Start recording with '{MicProfile.Name}'");

        micAmplifyMultiplier = micProfile.AmplificationMultiplier();

        // Code for low-latency microphone input taken from
        // https://support.unity3d.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        // It seems that there is still a latency of more than 200ms, which is too much for real-time processing.
        micAudioClip = UnityEngine.Microphone.Start(MicProfile.Name, true, 1, SampleRateHz);
        while (UnityEngine.Microphone.GetPosition(MicProfile.Name) <= 0) { /* Busy waiting */ }

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

        Debug.Log($"Stop recording with '{MicProfile.Name}'");
        UnityEngine.Microphone.End(MicProfile.Name);
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
        int currentSamplePosition = UnityEngine.Microphone.GetPosition(MicProfile.Name);
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

        ApplyMicAmplification(samplesSinceLastFrame);

        // Detect the pitch of the sample.
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(PitchDetectionBuffer, samplesSinceLastFrame, micProfile);

        // Notify listeners
        pitchEventStream.OnNext(pitchEvent);
    }

    private void ApplyMicAmplification(int samplesSinceLastFrame)
    {
        if (micAmplifyMultiplier == 0)
        {
            return;
        }
        float newSample;
        for (int index = 0; index < samplesSinceLastFrame - 1; index++)
        {
            newSample = PitchDetectionBuffer[index] * micAmplifyMultiplier;
            if (newSample > 1)
            {
                newSample = 1;
            }
            else if (newSample < -1)
            {
                newSample = -1;
            }
            PitchDetectionBuffer[index] = newSample;
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
}