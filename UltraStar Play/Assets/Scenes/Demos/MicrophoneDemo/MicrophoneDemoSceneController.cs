using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class MicrophoneDemoSceneController : MonoBehaviour
{
    public string micDeviceName = "Mikrofon (2- USB Microphone)";
    public Text currentNoteLabel;

    public FloatArrayVisualizer micDataVisualizer;
    public FloatArrayVisualizer pitchDetectionBufferVisualizer;

    private MicrophonePitchTracker microphonePitchTracker;

    private IDisposable pitchEventStreamDisposable;

    void Awake()
    {
        microphonePitchTracker = FindObjectOfType<MicrophonePitchTracker>();
        micDataVisualizer.Init(microphonePitchTracker.MicData);
        pitchDetectionBufferVisualizer.Init(microphonePitchTracker.PitchDetectionBuffer);
    }

    void OnEnable()
    {
        if (microphonePitchTracker == null)
        {
            return;
        }
        microphonePitchTracker.MicProfile = new MicProfile(micDeviceName);
        pitchEventStreamDisposable = microphonePitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
        microphonePitchTracker.StartPitchDetection();
    }

    void OnDisable()
    {
        if (microphonePitchTracker == null)
        {
            return;
        }
        pitchEventStreamDisposable?.Dispose();
        microphonePitchTracker.StopPitchDetection();
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
            currentNoteLabel.text = "Note: unkown";
        }
    }
}
