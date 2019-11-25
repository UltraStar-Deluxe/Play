using UnityEngine;
using UnityEngine.UI;

public class SongSelectMicListEntry : MonoBehaviour
{
    public Image micImage;

    public void Init(MicProfile micProfile)
    {
        MicrophonePitchTracker microphonePitchTracker = GetComponentInChildren<MicrophonePitchTracker>();
        FloatArrayVisualizer floatArrayVisualizer = GetComponentInChildren<FloatArrayVisualizer>();

        micImage.color = micProfile.Color;
        microphonePitchTracker.MicDevice = micProfile.Name;
        microphonePitchTracker.StartMicRecording();
        floatArrayVisualizer.Init(microphonePitchTracker.MicData);
    }
}
