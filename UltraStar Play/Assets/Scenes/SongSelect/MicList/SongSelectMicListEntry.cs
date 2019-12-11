using UnityEngine;
using UnityEngine.UI;

public class SongSelectMicListEntry : MonoBehaviour
{
    public Image micImage;

    private MicrophonePitchTracker microphonePitchTracker;
    private AudioWaveFormVisualizer audioWaveFormVisualizer;

    public void Init(MicProfile micProfile)
    {
        microphonePitchTracker = GetComponentInChildren<MicrophonePitchTracker>();
        audioWaveFormVisualizer = GetComponentInChildren<AudioWaveFormVisualizer>();

        micImage.color = micProfile.Color;
        microphonePitchTracker.MicProfile = micProfile;
        microphonePitchTracker.StartPitchDetection();
    }

    void Update()
    {
        if (audioWaveFormVisualizer != null && microphonePitchTracker != null)
        {
            float[] micData = microphonePitchTracker.MicData;
            audioWaveFormVisualizer.DrawWaveFormValues(micData, micData.Length - 1024, 1024);
        }
    }

}
