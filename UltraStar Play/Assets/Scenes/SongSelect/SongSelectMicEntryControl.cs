using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicEntryControl : IInjectionFinishedListener
{
    private static readonly int displayedSampleCount = 1024;

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
            micImage.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
        }
    }

    [Inject(UxmlName = R.UxmlNames.micIcon)]
    private VisualElement micImage;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    private readonly GameObject gameObject;
    private readonly VisualElement visualElement;
    private readonly MicPitchTracker micPitchTracker;

    private AudioWaveFormVisualization audioWaveFormVisualizer;

    public SongSelectMicEntryControl(GameObject gameObject, VisualElement visualElement, MicPitchTracker micPitchTracker)
    {
        this.gameObject = gameObject;
        this.visualElement = visualElement;
        this.micPitchTracker = micPitchTracker;
    }

    public void UpdateWaveForm()
    {
        if (audioWaveFormVisualizer != null)
        {
            float[] samples = micPitchTracker.MicSampleRecorder.MicSamples;
            audioWaveFormVisualizer.DrawWaveFormValues(samples, samples.Length - displayedSampleCount, displayedSampleCount);
        }
    }

    public void OnInjectionFinished()
    {
        // Size not available in first frame
        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            if (audioWaveForm is Label audioWaveFormLabel)
            {
                audioWaveFormLabel.text = "";
            }
            audioWaveFormVisualizer = new AudioWaveFormVisualization(gameObject, audioWaveForm);
        });
    }

    public void Destroy()
    {
        visualElement.RemoveFromHierarchy();
        audioWaveFormVisualizer.Destroy();
    }
}
