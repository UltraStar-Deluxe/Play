using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CalibrateMicDelayControl : MonoBehaviour, INeedInjection
{
    // The audio clips and midi notes that are played for calibration.
    [InjectedInInspector]
    public List<AudioClip> audioClips;

    [InjectedInInspector]
    public List<string> midiNoteNames;

    public MicProfile MicProfile { get; set; }

    private readonly Subject<CalibrationResult> calibrationResultEventStream = new();
    public IObservable<CalibrationResult> CalibrationResultEventStream => calibrationResultEventStream;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioSource;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private bool isCalibrationInProgress;

    private float startTimeInSeconds;
    private readonly float timeoutInSeconds = 2;
    private float pauseTime = float.MinValue;

    private List<int> delaysInMillis = new();
    private int currentIteration;

    void Awake()
    {
        // Sanity check
        if (midiNoteNames.Count == 0)
        {
            throw new UnityException("midiNoteNames not set");
        }
        if (audioClips.Count == 0)
        {
            throw new UnityException("audioClips not set");
        }
        if (audioClips.Count != midiNoteNames.Count)
        {
            throw new UnityException("audioClips and midiNotes must have same length");
        }
    }

    void Start()
    {
        micPitchTracker.PitchEventStream
            .Subscribe(OnPitchDetected);
        serverSideConnectRequestManager.ConnectedClientBeatPitchEventStream
            .Where(evt => evt.ClientId == MicProfile.ConnectedClientId)
            .Subscribe(OnPitchDetected);
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

    public void StartCalibration()
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
        Debug.Log("Mic delay calibration - timeout");
        audioSource.Stop();
        isCalibrationInProgress = false;
        calibrationResultEventStream.OnNext(new CalibrationResult
        {
            IsSuccess = false
        });
    }

    public void OnEndCalibration()
    {
        Debug.Log($"Mic delay calibration - median delay of {delaysInMillis.Count} values: {delaysInMillis[delaysInMillis.Count/2]}");
        audioSource.Stop();
        isCalibrationInProgress = false;

        calibrationResultEventStream.OnNext(new CalibrationResult
        {
            IsSuccess = true,
            DelaysInMilliseconds = new List<int>(delaysInMillis)
        });
    }

    private void StartIteration()
    {
        startTimeInSeconds = Time.time;
        audioSource.clip = audioClips[currentIteration];
        audioSource.Play();
    }

    private void OnPitchDetected(PitchEvent pitchEvent)
    {
        if (pitchEvent == null || !isCalibrationInProgress || pauseTime > 0)
        {
            return;
        }

        string targetMidiNoteName = midiNoteNames[currentIteration];
        if (MidiUtils.GetAbsoluteName(pitchEvent.MidiNote) == targetMidiNoteName)
        {
            audioSource.Stop();
            float delayInSeconds = Time.time - startTimeInSeconds;
            int delayInMillis = (int)(delayInSeconds * 1000);
            delaysInMillis.Add(delayInMillis);
            Debug.Log($"Mic delay calibration - delay of iteration {currentIteration}: {delayInMillis}");

            currentIteration++;
            if (currentIteration >= midiNoteNames.Count)
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
            Debug.Log("Mic delay calibration - wrong pitch: " + MidiUtils.GetAbsoluteName(pitchEvent.MidiNote));
        }
    }

    public class CalibrationResult
    {
        public bool IsSuccess { get; set; }
        public List<int> DelaysInMilliseconds { get; set; }
    }
}
