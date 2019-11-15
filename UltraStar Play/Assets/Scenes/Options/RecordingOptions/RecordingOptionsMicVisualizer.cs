using UnityEngine;
using UnityEngine.UI;

public class RecordingOptionsMicVisualizer : MonoBehaviour
{
    public Text currentNoteLabel;
    public FloatArrayVisualizer floatArrayVisualizer;
    public MicrophonePitchTracker microphonePitchTracker;

    void Start()
    {
        floatArrayVisualizer.Init(microphonePitchTracker.MicData);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        microphonePitchTracker.MicDevice = micProfile.Name;
        if (!string.IsNullOrEmpty(micProfile.Name))
        {
            microphonePitchTracker.StartPitchDetection();
        }
    }

    void OnEnable()
    {
        microphonePitchTracker.AddPitchDetectedHandler(OnPitchDetected);
    }

    void OnDisable()
    {
        microphonePitchTracker.RemovePitchDetectedHandler(OnPitchDetected);
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
            currentNoteLabel.text = "Note: ?";
        }
    }
}