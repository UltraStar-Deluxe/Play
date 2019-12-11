using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Collections;

public class RecordingOptionsMicVisualizer : MonoBehaviour
{
    public Text currentNoteLabel;
    public MicrophonePitchTracker microphonePitchTracker;

    private IDisposable pitchEventStreamDisposable;

    private AudioWaveFormVisualizer audioWaveFormVisualizer;

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
        float[] micData = microphonePitchTracker.MicData;
        audioWaveFormVisualizer.DrawWaveFormValues(micData, micData.Length - 2048, 2048);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        microphonePitchTracker.MicProfile = micProfile;
        if (!string.IsNullOrEmpty(micProfile.Name))
        {
            microphonePitchTracker.StartPitchDetection();
        }
    }

    void OnEnable()
    {
        pitchEventStreamDisposable = microphonePitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
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