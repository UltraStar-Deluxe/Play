using System;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class RecordingOptionsMicVisualizer : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private MicPitchTracker micPitchTracker;

    [Inject(UxmlName = R.UxmlNames.noteLabel)]
    private Label currentNoteLabel;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    private IDisposable pitchEventStreamDisposable;

    private void Start()
    {
        pitchEventStreamDisposable = micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
        serverSideConnectRequestManager.ConnectedClientBeatPitchEventStream
            .Where(connectedClientBeatPitchEvent => connectedClientBeatPitchEvent.ClientId == micPitchTracker.MicProfile.ConnectedClientId)
            .Subscribe(OnPitchDetected);
        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(this.gameObject, audioWaveForm);
        });
    }

    private void Update()
    {
        UpdateWaveForm();
    }

    private void OnDestroy()
    {
        pitchEventStreamDisposable?.Dispose();
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

        // Consider noise suppression when displaying the the buffer
        float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;
        float noiseThreshold = micProfile.NoiseSuppression / 100f;
        bool micSampleBufferIsAboveThreshold = micData.AnyMatch(sample => sample >= noiseThreshold);
        float[] displayData = micSampleBufferIsAboveThreshold
            ? micData
            : new float[micData.Length];
        audioWaveFormVisualization.DrawWaveFormMinAndMaxValues(displayData);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        micPitchTracker.MicProfile = micProfile;
        if (!string.IsNullOrEmpty(micProfile.Name))
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
