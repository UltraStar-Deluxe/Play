using System;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class RecordingOptionsMicVisualizer : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private MicSampleRecorder micSampleRecorder;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private MicPitchTracker micPitchTracker;

    [Inject(UxmlName = R.UxmlNames.noteLabel)]
    private Label currentNoteLabel;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject]
    private RecordingOptionsSceneControl recordingOptionsSceneControl;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private readonly float[] emptySamplesArray = new float[2];
    
    private void Start()
    {
        micPitchTracker.PitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
        recordingOptionsSceneControl.ConnectedClientBeatPitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(this.gameObject, audioWaveForm);
        });
    }

    private void Update()
    {
        UpdateWaveForm();
        micPitchTracker.AudioSamplesAnalyzer.ModifySamplesInPlace = false;
    }

    private void UpdateWaveForm()
    {
        if (audioWaveFormVisualization == null)
        {
            return;
        }

        MicProfile micProfile = micPitchTracker.MicProfile;
        if (micProfile == null)
        {
            return;
        }

        float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;

        // Consider amplification
        AbstractAudioSamplesAnalyzer.ApplyAmplification(micData, 0, micData.Length, micProfile.AmplificationMultiplier);

        // Consider noise suppression when displaying the the buffer
        bool isAboveThreshold = AbstractAudioSamplesAnalyzer.IsAboveNoiseSuppressionThreshold(micData, 0, micData.Length, micProfile.NoiseSuppression);
        float[] displayData = isAboveThreshold
            ? micData
            : emptySamplesArray;

        audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(displayData);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        micPitchTracker.MicProfile = micProfile;
        if (!micProfile.Name.IsNullOrEmpty()
            && !micProfile.IsInputFromConnectedClient)
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        // Show the note that has been detected
        if (pitchEvent != null && pitchEvent.MidiNote > 0)
        {
            currentNoteLabel.text = TranslationManager.GetTranslation(R.Messages.options_note,
                "value", MidiUtils.GetAbsoluteName(pitchEvent.MidiNote));
        }
        else
        {
            currentNoteLabel.text = TranslationManager.GetTranslation(R.Messages.options_note,
                "value", "?");
        }
    }
}
