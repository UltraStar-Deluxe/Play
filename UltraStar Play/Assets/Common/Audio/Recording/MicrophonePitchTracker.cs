using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pitch;
using UnityEngine;
using static Pitch.PitchTracker;

[RequireComponent(typeof(AudioSource))]
public class MicrophonePitchTracker : MonoBehaviour
{
    private const int SampleRate = 22050;

    public bool playRecordedAudio;

    public string MicDevice { get; set; }
    public float[] MicData { get; private set; } = new float[SampleRate];
    public float[] PitchDetectionBuffer { get; private set; } = new float[SampleRate];

    private AudioSource audioSource;
    private AudioClip micAudioClip;

    private PitchTracker pitchTracker = new PitchTracker();
    private bool startedPitchDetection;

    public delegate void PitchDetectedHandler(int midiNote);
    public event PitchDetectedHandler PitchDetected;

    [Range(1, 20)]
    public int pitchRecordHistoryLength = 5;
    private List<PitchRecord> pitchRecordHistory = new List<PitchRecord>();

    [ReadOnly]
    public string lastMidiNoteName;

    private int lastRecordedFrame;

    public void AddPitchDetectedHandler(PitchDetectedHandler handler)
    {
        PitchDetected += new PitchDetectedHandler(handler);
    }

    public void RemovePitchDetectedHandler(PitchDetectedHandler handler)
    {
        PitchDetected -= new PitchDetectedHandler(handler);
    }

    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        // Initialize the pitch tracker
        pitchTracker.PitchRecordsPerSecond = 30;
        pitchTracker.RecordPitchRecords = true;
        pitchTracker.PitchRecordHistorySize = 5;
        pitchTracker.SampleRate = SampleRate;

        pitchTracker.PitchDetected += new PitchTracker.PitchDetectedHandler(OnPitchDetected);
    }

    void OnDisable()
    {
        pitchTracker.PitchDetected -= new PitchTracker.PitchDetectedHandler(OnPitchDetected);
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
        while (Microphone.GetPosition(MicDevice) <= 0) { /* Busy waiting */ }

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
        pitchTracker.ProcessBuffer(PitchDetectionBuffer, samplesSinceLastFrame);
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

    private void OnPitchDetected(PitchTracker sender, PitchRecord pitchRecord)
    {
        // Ignore multiple events in same frame.
        if (lastRecordedFrame == Time.frameCount)
        {
            return;
        }
        lastRecordedFrame = Time.frameCount;

        // Create history of PitchRecord events
        pitchRecordHistory.Add(pitchRecord);
        while (pitchRecordHistoryLength > 0 && pitchRecordHistory.Count > pitchRecordHistoryLength)
        {
            pitchRecordHistory.RemoveAt(0);
        }

        // Calculate median of recorded midi note values.
        // This is done to make the pitch detection more stable, but it increases the latency.
        List<PitchRecord> sortedpitchRecordHistory = new List<PitchRecord>(pitchRecordHistory);
        sortedpitchRecordHistory.Sort(new PitchRecordComparer());
        int midiNoteMedian = sortedpitchRecordHistory[sortedpitchRecordHistory.Count / 2].MidiNote;
        PitchDetected(midiNoteMedian);

        // Update label in inspector for debugging.
        if (midiNoteMedian > 0)
        {
            lastMidiNoteName = MidiUtils.GetAbsoluteName(midiNoteMedian);
        }
        else
        {
            lastMidiNoteName = "";
        }
    }

    private class PitchRecordComparer : IComparer<PitchRecord>
    {
        public int Compare(PitchRecord x, PitchRecord y)
        {
            return x.MidiNote.CompareTo(y.MidiNote);
        }
    }
}
