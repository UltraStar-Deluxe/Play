using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CalibrateMicDelayButton : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public MicrophonePitchTracker micPitchTracker;

    [Inject]
    private MidiManager midiManager;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private MicDelaySlider micDelaySlider;

    // C3 = 48, C4 = 60, C5 = 72
    private int[] midiNotes = new int[] { 48, 60, 72 };

    private bool isCalibrationInProgress;

    private float startTimeInSeconds;
    private readonly float timeoutInSeconds = 2;
    private float pauseTime = float.MinValue;

    private List<int> delaysInMillis = new List<int>();
    private int currentIteration;

    public MicProfile MicProfile { get; set; }

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => OnStartCalibration());
        micPitchTracker.PitchEventStream.Subscribe(OnPitchDetected);
    }

    void Update()
    {
        if (!isCalibrationInProgress)
        {
            return;
        }

        if (pauseTime > 0)
        {
            pauseTime -= Time.deltaTime;
        }
        else if (pauseTime > float.MinValue)
        {
            pauseTime = float.MinValue;
            StartIteration();
        }

        if ((startTimeInSeconds + timeoutInSeconds) < Time.time)
        {
            OnCalibrationTimedOut();
        }
    }

    private void OnStartCalibration()
    {
        if (isCalibrationInProgress)
        {
            return;
        }
        isCalibrationInProgress = true;

        delaysInMillis = new List<int>();
        currentIteration = 0;
        StartIteration();
    }

    private void OnCalibrationTimedOut()
    {
        Debug.Log("Mic delay calibration timed out");
        midiManager.StopAllMidiNotes();
        isCalibrationInProgress = false;
    }

    private void OnEndCalibration()
    {
        Debug.Log($"avg delay of {delaysInMillis.Count} values: {delaysInMillis.Average()}");
        midiManager.StopAllMidiNotes();
        isCalibrationInProgress = false;

        if (MicProfile != null)
        {
            MicProfile.DelayInMillis = (int)delaysInMillis.Average();
            micDelaySlider.SetMicProfile(MicProfile);
        }
    }

    private void StartIteration()
    {
        startTimeInSeconds = Time.time;
        midiManager.PlayMidiNote(midiNotes[currentIteration]);
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (pitchEvent == null || !isCalibrationInProgress || pauseTime > 0)
        {
            return;
        }

        if (Math.Abs(pitchEvent.MidiNote - midiNotes[currentIteration]) < 4)
        {
            midiManager.StopAllMidiNotes();
            float delayInSeconds = Time.time - startTimeInSeconds;
            int delayInMillis = (int)(delayInSeconds * 1000);
            delaysInMillis.Add(delayInMillis);
            Debug.Log($"delay of iteration {currentIteration}: {delayInMillis}");

            currentIteration++;
            Debug.Log($"iter: {currentIteration}, length: {midiNotes.Length}");
            if (currentIteration >= midiNotes.Length)
            {
                OnEndCalibration();
            }
            else
            {
                // Wait a bit for silence before the next iteration.
                pauseTime = 0.5f;
            }
        }
        else
        {
            Debug.Log("Wrong pitch: " + pitchEvent.MidiNote);
        }
    }
}
