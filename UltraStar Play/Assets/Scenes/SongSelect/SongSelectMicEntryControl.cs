using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectMicEntryControl : IInjectionFinishedListener, IDisposable, INeedInjection
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
    private readonly NewestSamplesMicPitchTracker micPitchTracker;

    private AudioWaveFormVisualization audioWaveFormVisualizer;

    public SongSelectMicEntryControl(GameObject gameObject, VisualElement visualElement, NewestSamplesMicPitchTracker micPitchTracker)
    {
        this.gameObject = gameObject;
        this.visualElement = visualElement;
        this.micPitchTracker = micPitchTracker;
    }

    public void UpdateWaveForm()
    {
        if (audioWaveFormVisualizer != null)
        {
            float[] samples = micPitchTracker.MicSamples;
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
                audioWaveFormLabel.SetTranslatedText(Translation.Empty);
            }
            int textureWidth = 256;
            int textureHeight = 128;
            audioWaveFormVisualizer = new AudioWaveFormVisualization(
                gameObject,
                audioWaveForm,
                textureWidth,
                textureHeight,
                $"song select mic audio visualization of '{micProfile.GetDisplayNameWithChannel()}'");
        });
    }

    public void Dispose()
    {
        visualElement.RemoveFromHierarchy();
        audioWaveFormVisualizer.Dispose();
    }
}
