using System;
using UnityEngine;
using UnityEngine.UI;
using Pitch;

public class MicrophoneDemoSceneController : MonoBehaviour
{
    public string micDeviceName = "Mikrofon (2- USB Microphone)";
    public Text currentNoteLabel;

    private MicrophonePitchTracker microphonePitchTracker;

    public FloatArrayVisualizer micDataVisualizer;
    public FloatArrayVisualizer pitchDetectionBufferVisualizer;

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
        microphonePitchTracker.MicDevice = micDeviceName;
        microphonePitchTracker.AddPitchDetectedHandler(OnPitchDetected);
        microphonePitchTracker.StartPitchDetection();
    }

    void OnDisable()
    {
        if (microphonePitchTracker == null)
        {
            return;
        }
        microphonePitchTracker.RemovePitchDetectedHandler(OnPitchDetected);
        microphonePitchTracker.StopPitchDetection();
    }

    private void OnPitchDetected(int midiNote)
    {
        // Show the note that has been detected
        if (midiNote > 0)
        {
            currentNoteLabel.text = "Note: " + MidiUtils.GetAbsoluteName(midiNote);
        }
        else
        {
            currentNoteLabel.text = "Note: unkown";
        }
    }
}
