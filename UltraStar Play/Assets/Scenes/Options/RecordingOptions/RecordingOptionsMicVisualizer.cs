using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Collections;

public class RecordingOptionsMicVisualizer : MonoBehaviour
{
    public Text currentNoteLabel;
    public MicPitchTracker micPitchTracker;

    private IDisposable pitchEventStreamDisposable;

    private AudioWaveFormVisualizer audioWaveFormVisualizer;

    private IDisposable disposable;

    void Awake()
    {
        audioWaveFormVisualizer = GetComponentInChildren<AudioWaveFormVisualizer>();
    }

    void Update()
    {
        UpdateWaveForm();
    }

    private void UpdateWaveForm()
    {
        MicProfile micProfile = micPitchTracker.MicProfile;
        if (micProfile == null)
        {
            return;
        }

        // Consider noise suppression when displaying the the buffer
        float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;
        float noiseThreshold = micProfile.NoiseSuppression / 100f;
        bool micSampleBufferIsAboveThreshold = micData.AnyMatch(sample => sample >= noiseThreshold);
        float[] displayData = micSampleBufferIsAboveThreshold
            ? micData
            : new float[micData.Length];
        audioWaveFormVisualizer.DrawWaveFormMinAndMaxValues(displayData);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        micPitchTracker.MicProfile = micProfile;
        if (!string.IsNullOrEmpty(micProfile.Name))
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }

        if (disposable != null)
        {
            disposable.Dispose();
        }
    }

    void OnEnable()
    {
        pitchEventStreamDisposable = micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
    }

    void OnDisable()
    {
        pitchEventStreamDisposable?.Dispose();
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        // Show the note that has been detected
        if (pitchEvent != null && pitchEvent.MidiNote > 0)
        {
            currentNoteLabel.text = "Note: " + MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
        }
        else
        {
            currentNoteLabel.text = "Note: ?";
        }
    }
}
