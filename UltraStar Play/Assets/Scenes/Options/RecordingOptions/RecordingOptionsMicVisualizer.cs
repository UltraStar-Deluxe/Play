using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class RecordingOptionsMicVisualizer : MonoBehaviour, INeedInjection
{
    [Inject]
    private NewestSamplesMicPitchTracker micPitchTracker;

    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.noteLabel)]
    private Label noteLabel;

    [Inject(UxmlName = R.UxmlNames.pitchIndicator)]
    private VisualElement pitchIndicator;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject]
    private RecordingOptionsSceneControl recordingOptionsSceneControl;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private readonly float[] emptySamplesArray = new float[16000];

    private void Start()
    {
        micPitchTracker.PitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
        recordingOptionsSceneControl.CompanionClientBeatPitchEventStream
            .Subscribe(OnPitchDetected)
            .AddTo(gameObject);
        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            int textureWidth = 256;
            int textureHeight = 128;
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                gameObject,
                audioWaveForm,
                textureWidth,
                textureHeight,
                "recording options audio visualization");
        });
    }

    private void Update()
    {
        UpdateWaveForm();
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

        float[] micData = micPitchTracker.MicSamples;

        // Consider amplification
        AbstractAudioSamplesAnalyzer.ApplyAmplification(micData, 0, micData.Length, micProfile.AmplificationMultiplier);

        // Consider noise suppression when displaying the the buffer
        bool isAboveThreshold = AbstractAudioSamplesAnalyzer.IsAboveNoiseSuppressionThreshold(micData, 0, micData.Length, micProfile.NoiseSuppression);
        float[] displayData = isAboveThreshold
            ? micData
            : emptySamplesArray;

        audioWaveFormVisualization.DrawAudioWaveForm(displayData);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        micPitchTracker.StopRecording();

        micPitchTracker.MicProfile = micProfile;
        if (!micProfile.Name.IsNullOrEmpty()
            && !micProfile.IsInputFromConnectedClient
            && micProfile.IsEnabledAndConnected(ServerSideCompanionClientManager.Instance))
        {
            micPitchTracker.StartRecording();
        }
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        // Show the note that has been detected
        if (pitchEvent != null && pitchEvent.MidiNote > 0)
        {
            noteLabel.SetTranslatedText(Translation.Get(R.Messages.options_note,
                "value", MidiUtils.GetAbsoluteName(pitchEvent.MidiNote)));

            float midiNoteFactor = ((float)pitchEvent.MidiNote - MidiUtils.SingableNoteMin) / (MidiUtils.SingableNoteRange);
            pitchIndicator.style.top = new StyleLength(Length.Percent(100 - (100 * midiNoteFactor)));
        }
        else
        {
            noteLabel.SetTranslatedText(Translation.Get(R.Messages.options_note,
                "value", "?"));
        }
    }
}
