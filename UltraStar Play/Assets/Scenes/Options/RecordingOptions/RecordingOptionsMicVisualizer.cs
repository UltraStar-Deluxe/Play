using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class RecordingOptionsMicVisualizer : MonoBehaviour
{
    public Text currentNoteLabel;
    public FloatArrayVisualizer floatArrayVisualizer;
    public MicrophonePitchTracker microphonePitchTracker;

    private IDisposable pitchEventStreamDisposable;

    void Start()
    {
        floatArrayVisualizer.Init(microphonePitchTracker.MicData);
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