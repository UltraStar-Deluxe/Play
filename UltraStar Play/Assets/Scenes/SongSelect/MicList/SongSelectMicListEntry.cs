using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicListEntry : MonoBehaviour, INeedInjection
{
    private static readonly int displayedSampleCount = 1024;
    
    [InjectedInInspector]
    public Image micImage;

    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get
        {
            return micProfile;
        }
        
        set
        {
            micProfile = value;
            micImage.color = micProfile.Color;
            micPitchTracker.MicProfile = micProfile;
            micPitchTracker.MicSampleRecorder.StartRecording();
        }
    }
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private MicPitchTracker micPitchTracker;
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioWaveFormVisualizer audioWaveFormVisualizer;
    
    void Update()
    {
        if (audioWaveFormVisualizer != null && micPitchTracker != null)
        {
            float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;
            audioWaveFormVisualizer.DrawWaveFormValues(micData, micData.Length - displayedSampleCount, displayedSampleCount);
        }
    }
}
