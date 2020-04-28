using UnityEngine;
using UnityEngine.UI;

public class SongSelectMicListEntry : MonoBehaviour
{
    public Image micImage;

    private MicPitchTracker micPitchTracker;
    private AudioWaveFormVisualizer audioWaveFormVisualizer;

    public void Init(MicProfile micProfile)
    {
        micPitchTracker = GetComponentInChildren<MicPitchTracker>();
        audioWaveFormVisualizer = GetComponentInChildren<AudioWaveFormVisualizer>();

        micImage.color = micProfile.Color;
        micPitchTracker.MicProfile = micProfile;
        micPitchTracker.MicSampleRecorder.StartRecording();
    }

    void Update()
    {
        if (audioWaveFormVisualizer != null && micPitchTracker != null)
        {
            float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;
            audioWaveFormVisualizer.DrawWaveFormValues(micData, micData.Length - 1024, 1024);
        }
    }

}
