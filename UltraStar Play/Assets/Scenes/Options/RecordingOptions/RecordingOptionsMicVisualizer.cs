using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Collections;

public class RecordingOptionsMicVisualizer : MonoBehaviour
{
    public Text currentNoteLabel;
    public MicPitchTracker micPitchTracker;

    private const int DisplayedSampleCount = 44100;

    private IDisposable pitchEventStreamDisposable;

    private AudioWaveFormVisualizer audioWaveFormVisualizer;

    private float micAmplifyMultiplier = 1;

    private IDisposable disposable;

    void Awake()
    {
        audioWaveFormVisualizer = GetComponentInChildren<AudioWaveFormVisualizer>();
    }

    void Update()
    {
        UpdateWaveForm();
    }

    private void UpdateWaveForm()
    {
        MicProfile micProfile = micPitchTracker.MicProfile;
        if (micProfile == null)
        {
            return;
        }

        float[] micData = micPitchTracker.MicSampleRecorder.MicSamples;

        // Apply noise suppression and amplification to the buffer
        float[] displayData = new float[micData.Length];
        float noiseThreshold = micProfile.NoiseSuppression / 100f;
        if (micData.AnyMatch(sample => sample >= noiseThreshold))
        {
            for (int i = 0; i < micData.Length; i++)
            {
                displayData[i] = NumberUtils.Limit(micData[i] * micAmplifyMultiplier, -1, 1);
            }
        }

        audioWaveFormVisualizer.DrawWaveFormValues(displayData, 0, micData.Length);
    }

    public void SetMicProfile(MicProfile micProfile)
    {
        micPitchTracker.MicProfile = micProfile;
        if (!string.IsNullOrEmpty(micProfile.Name))
        {
            micPitchTracker.MicSampleRecorder.StartRecording();
        }
        micAmplifyMultiplier = micProfile.AmplificationMultiplier;

        if (disposable != null)
        {
            disposable.Dispose();
        }
        disposable = micProfile.ObserveEveryValueChanged(it => it.Amplification)
            .Subscribe(newAmplification => micAmplifyMultiplier = micProfile.AmplificationMultiplier);
    }

    void OnEnable()
    {
        pitchEventStreamDisposable = micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
    }

    void OnDisable()
    {
        pitchEventStreamDisposable?.Dispose();
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        // Show the note that has been detected
        if (pitchEvent != null && pitchEvent.MidiNote > 0)
        {
            currentNoteLabel.text = "Note: " + MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
        }
        else
        {
            currentNoteLabel.text = "Note: ?";
        }
    }
}
