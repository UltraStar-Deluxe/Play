using UnityEngine;
using UnityEngine.UI;

public class SongSelectMicListEntry : MonoBehaviour
{
    public Image micImage;

    public void Init(MicProfile micProfile)
    {
        AbstractMicPitchTracker microphonePitchTracker = GetComponentInChildren<AbstractMicPitchTracker>();
        FloatArrayVisualizer floatArrayVisualizer = GetComponentInChildren<FloatArrayVisualizer>();

        micImage.color = micProfile.Color;
        microphonePitchTracker.MicDevice = micProfile.Name;
        microphonePitchTracker.StartMicRecording();
        floatArrayVisualizer.Init(microphonePitchTracker.MicData);
    }
}
