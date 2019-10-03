using System;
using UnityEngine;
using UnityEngine.UI;
using Pitch;

public class MicrophoneDemoSceneController : MonoBehaviour
{
    public string micDeviceName = "Mikrofon (2- USB Microphone)";
    public Text currentNoteLabel;

    private MicrophonePitchTracker microphonePitchTracker;

    void Awake()
    {
        microphonePitchTracker = FindObjectOfType<MicrophonePitchTracker>();
        FloatArrayVisualizer floatArrayVisualizer = FindObjectOfType<FloatArrayVisualizer>();
        floatArrayVisualizer.Init(microphonePitchTracker.MicData);
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

    private void OnPitchDetected(PitchTracker sender, PitchTracker.PitchRecord pitchRecord)
    {
        // Show the note that has been detected
        int midiNote = pitchRecord.MidiNote;
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
