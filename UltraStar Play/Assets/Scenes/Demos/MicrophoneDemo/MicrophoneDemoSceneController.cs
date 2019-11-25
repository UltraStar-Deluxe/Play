using System;
using UnityEngine;
using UnityEngine.UI;
using Pitch;
using UniRx;

public class MicrophoneDemoSceneController : MonoBehaviour
{
    public string micDeviceName = "Mikrofon (2- USB Microphone)";
    public Text currentNoteLabel;

    public FloatArrayVisualizer micDataVisualizer;
    public FloatArrayVisualizer pitchDetectionBufferVisualizer;

    private AbstractMicPitchTracker micPitchTracker;

    private IDisposable pitchEventStreamDisposable;

    void Awake()
    {
        if (micPitchTracker == null)
        {
            micPitchTracker = FindObjectOfType<AbstractMicPitchTracker>();
        }
        if (micPitchTracker == null)
        {
            throw new UnityException("No microphone pitch tracker found in the scene");
        }
        micDataVisualizer.Init(micPitchTracker.MicData);
        pitchDetectionBufferVisualizer.Init(micPitchTracker.PitchDetectionBuffer);
    }

    void OnEnable()
    {
        if (micPitchTracker == null)
        {
            return;
        }
        micPitchTracker.MicDevice = micDeviceName;
        pitchEventStreamDisposable = micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
        micPitchTracker.StartMicRecording();
    }

    void OnDisable()
    {
        if (micPitchTracker == null)
        {
            return;
        }
        micPitchTracker.StopMicRecording();
        pitchEventStreamDisposable?.Dispose();
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        // Show the note that has been detected
        int midiNote = pitchEvent.MidiNote;
        if (midiNote > 0)
        {
            currentNoteLabel.text = "Note: " + MidiUtils.GetAbsoluteName(midiNote) + " (" + midiNote + ")";
        }
        else
        {
            currentNoteLabel.text = "Note: unkown";
        }
    }
}
